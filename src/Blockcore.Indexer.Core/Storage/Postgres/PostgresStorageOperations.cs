
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.NBitcoin;
using Blockcore.Consensus.TransactionInfo;
using Transaction = Blockcore.Indexer.Core.Storage.Postgres.Types.Transaction;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Blockcore.Indexer.Core.Storage.Postgres
{
    public class PostgresStorageOperations : IStorageOperations
    {
        const string OpReturnAddress = "TX_NULL_DATA";
        protected readonly SyncConnection syncConnection;
        protected readonly GlobalState globalState;
        protected readonly IScriptInterpreter scriptInterpeter;
        protected readonly IndexerSettings configuration;
        protected readonly PostgresDbContext db;
        protected readonly IStorage storage;
        //todo -> change to generic interface
        protected readonly IMapPgBlockToStorageBlock pgBlockToStorageBlock;
        public PostgresStorageOperations(
            SyncConnection connection,
            IStorage storage,
            IOptions<IndexerSettings> configuration,
            GlobalState globalState,
            IMapPgBlockToStorageBlock pgBlockToStorageBlock,
            IScriptInterpreter scriptInterpeter,
            PostgresDbContext context)
        {
            syncConnection = connection;
            this.storage = storage;
            this.globalState = globalState;
            this.pgBlockToStorageBlock = pgBlockToStorageBlock;
            this.scriptInterpeter = scriptInterpeter;
            db = context;
            this.configuration = configuration.Value;
        }

        public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
        {
            var postgresStorageBatch = storageBatch as PostgresStorageBatch;

            postgresStorageBatch.TotalSize += item.BlockInfo.Size;
            Block block = pgBlockToStorageBlock.Map(item.BlockInfo);

            int transactionIndex = 0;
            foreach (var trx in item.Transactions)
            {
                string txid = trx.GetHash().ToString();
                Transaction transaction = new()
                {
                    BlockIndex = item.BlockInfo.HeightAsUint32,
                    Txid = txid,
                    TransactionIndex = transactionIndex++,
                    RawTransaction = configuration.StoreRawTransactions ? trx.ToBytes(syncConnection.Network.Consensus.ConsensusFactory) : null,
                    Inputs = [],
                    Outputs = []
                };

                int outputIndex = 0;
                foreach (TxOut output in trx.Outputs)
                {
                    ScriptOutputInfo res = scriptInterpeter.InterpretScript(syncConnection.Network, output.ScriptPubKey);
                    string addr = res != null
                       ? (res?.Addresses != null && res.Addresses.Any()) ? res.Addresses.First() : res.ScriptType
                       : "none";

                    var outpoint = new Outpoint { Txid = txid, Vout = outputIndex++ };
                    var storageOutput = new Output
                    {
                        outpoint = outpoint,
                        Address = addr,
                        BlockIndex = item.BlockInfo.HeightAsUint32,
                        Value = output.Value,
                        ScriptHex = output.ScriptPubKey.ToHex(),
                        CoinBase = trx.IsCoinBase,
                        CoinStake = syncConnection.Network.Consensus.IsProofOfStake && trx.IsCoinStake
                    };

                    transaction.Outputs.Add(storageOutput);
                    postgresStorageBatch.Outputs.Add(outpoint.ToString(), storageOutput);
                }

                if (trx.IsCoinBase)
                    continue;

                int inputIndex = 0;
                foreach (TxIn input in trx.Inputs)
                {
                    var outpoint = new Types.Outpoint { Txid = input.PrevOut.Hash.ToString(), Vout = (int)input.PrevOut.N };
                    postgresStorageBatch.Outputs.TryGetValue(outpoint.ToString(), out Types.Output output);

                    Input storageInput = new Input
                    {
                        outpoint = outpoint,
                        Txid = txid,
                        BlockIndex = item.BlockInfo.HeightAsUint32,
                        Value = output?.Value ?? 0,
                    };
                    transaction.Inputs.Add(storageInput);
                    postgresStorageBatch.Inputs.Add(storageInput);
                }

                block.Transactions.Add(transaction);
                block.TransactionCount++;
            }
            OnAddtoStorageBatch(postgresStorageBatch, item);
            postgresStorageBatch.Blocks.Add(item.BlockInfo.Height, pgBlockToStorageBlock.Map(item.BlockInfo));
        }

        public Storage.Types.SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
        {
            var postgresStorageBatch = storageBatch as PostgresStorageBatch;
            if (globalState.IndexModeCompleted)
            {
                if (globalState.IbdMode() == false)
                {
                    if (globalState.LocalMempoolView.Any())
                    {
                        var toRemoveFromMempool = postgresStorageBatch.Blocks.Values.SelectMany(b => b.Transactions.Select(t => t.Txid));
                        Task deleteFromMempoolTask = Task.Run(async () =>
                        {
                            await db.mempoolTransactions.Where(mt => toRemoveFromMempool.Contains(mt.Txid)).ExecuteDeleteAsync();
                        });
                        deleteFromMempoolTask.Wait();

                        foreach (string mempooltrx in toRemoveFromMempool)
                        {
                            globalState.LocalMempoolView.Remove(mempooltrx, out _);
                        }
                    }
                }
            }


            var utxos = new List<UnspentOutput>(postgresStorageBatch.Outputs.Values.Count);

            foreach (Output output in postgresStorageBatch.Outputs.Values)
            {
                if (output.Address.Equals(OpReturnAddress))
                    continue;

                // TODO: filter out outputs that are already spent in the storageBatch.InputTable table
                // such inputs will get deleted anyway in the next operation of UnspentOutputTable.DeleteMany
                // this means we should probably make the storageBatch.InputTable a dictionary as well.

                utxos.Add(new UnspentOutput
                {
                    Address = output.Address,
                    Outpoint = output.outpoint,
                    Value = output.Value,
                    BlockIndex = output.BlockIndex,
                });
            }

            Task utxoInsertTask = utxos.Any() ? Task.Run(async () => await db.BulkInsertAsync(utxos))
                                    : Task.CompletedTask;

            if (postgresStorageBatch.Inputs.Any())
            {
                var utxoLookups = FetchUtxos(postgresStorageBatch.Inputs
                    .Where(_ => _.Address == null)
                    .Select(_ => _.outpoint));

                foreach (Input input in postgresStorageBatch.Inputs)
                {
                    if (input.Address != null)
                    {
                        continue;
                    }

                    string key = input.outpoint.ToString();
                    input.Address = utxoLookups[key].Address;
                    input.Value = utxoLookups[key].Value;
                }
            }

            Task pushBatchToPostgresTask = Task.Run(async () =>
            {
                await db.BulkInsertAsync(postgresStorageBatch.Blocks.Values, options => { options.IncludeGraph = true; });
            });

            Task.WaitAll(pushBatchToPostgresTask, utxoInsertTask);

            if (postgresStorageBatch.Inputs.Any())
            {
                // TODO: if earlier we filtered out outputs that are already spent and not pushed to the utxo table
                // now we do not need to try and delete such outputs becuase they where never pushed to the store.
                var outpointsFromNewInput = postgresStorageBatch.Inputs.Select(_ => _.outpoint).ToList();

                int rowsDeleted = db.unspentOutputs.Where(uo => outpointsFromNewInput.Contains(uo.Outpoint)).ExecuteDelete();

                if (rowsDeleted != outpointsFromNewInput.Count)
                {
                    throw new ApplicationException($"Delete of unspent outputs did not complete successfully : {rowsDeleted} deleted but {outpointsFromNewInput.Count} expected");
                }
            }

            OnPushStorageBatch(postgresStorageBatch);

            string lastBlockHash = null;
            long blockIndex = 0;
            List<Block> markBlocksAsComplete = [];
            foreach (Block mapBlock in postgresStorageBatch.Blocks.Values.OrderBy(b => b.BlockIndex))
            {
                mapBlock.SyncComplete = true;
                markBlocksAsComplete.Add(mapBlock);
                lastBlockHash = mapBlock.BlockHash;
                blockIndex = mapBlock.BlockIndex;
            }

            db.BulkUpdate(markBlocksAsComplete);

            Storage.Types.SyncBlockInfo block = storage.BlockByIndex(blockIndex);

            if (block.BlockHash != lastBlockHash)
            {
                throw new ArgumentException($"Expected hash {blockIndex} for block {lastBlockHash} but was {block.BlockHash}");
            }

            return block;
        }

        public void InsertMempoolTransactions(SyncBlockTransactionsOperation item)
        {
            var mempool = new List<MempoolTransaction>();
            var inputs = new Dictionary<string, (MempoolInput mempoolInput, MempoolTransaction mempool)>();

            foreach (var itemTransaction in item.Transactions)
            {
                var mempoolEntry = new MempoolTransaction() { Txid = itemTransaction.GetHash().ToString(), FirstSeen = DateTime.UtcNow.Ticks };
                mempool.Add(mempoolEntry);

                foreach (TxOut transactionOutput in itemTransaction.Outputs)
                {
                    ScriptOutputInfo res =
                       scriptInterpeter.InterpretScript(syncConnection.Network, transactionOutput.ScriptPubKey);
                    string addr = res != null
                       ? (res?.Addresses != null && res.Addresses.Any()) ? res.Addresses.First() : res.ScriptType.ToString()
                       : null;

                    if (addr != null)
                    {
                        var output = new MempoolOutput
                        {
                            Value = transactionOutput.Value,
                            ScriptHex = transactionOutput.ScriptPubKey.ToHex(),
                            Address = addr
                        };
                        mempoolEntry.Outputs.Add(output);
                        mempoolEntry.AddressOutputs.Add(addr);
                    }
                }

                foreach (TxIn transactionInput in itemTransaction.Inputs)
                {
                    var input = new MempoolInput
                    {
                        outpoint = new Outpoint
                        {
                            Vout = (int)transactionInput.PrevOut.N,
                            Txid = transactionInput.PrevOut.Hash.ToString()
                        }
                    };
                    mempoolEntry.Inputs.Add(input);
                    inputs.Add($"{input.outpoint.Txid}-{input.outpoint.Vout}", (input, mempoolEntry));
                }
            }

            List<Output> outputsFromStore = FetchOutputs(inputs.Values.Select(s => s.mempoolInput.outpoint).ToList());

            foreach (Output outputFromStore in outputsFromStore)
            {
                if (inputs.TryGetValue($"{outputFromStore.outpoint.Txid}-{outputFromStore.outpoint.Vout}",
                       out (MempoolInput mempoolInput, MempoolTransaction mempool) input))
                {
                    input.mempoolInput.Address = outputFromStore.Address;
                    input.mempoolInput.Value = outputFromStore.Value;
                    input.mempool.AddressInputs.Add(outputFromStore.Address);
                }
                else
                {
                    // output not found
                }
            }
            Task insertToMempoolTask = Task.Run(async () => await db.BulkInsertAsync(mempool, options => options.IncludeGraph = true));
            try
            {
                Task.WaitAll(insertToMempoolTask);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            foreach (MempoolTransaction mempooltrx in mempool)
                globalState.LocalMempoolView.TryAdd(mempooltrx.Txid, string.Empty);
        }
        protected virtual void OnAddtoStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item){

        }
        private void OnPushStorageBatch(StorageBatch storageBatch) { }
        private List<Output> FetchOutputs(List<Outpoint> outpoints){
            var res = db.Outputs.Where(output => outpoints.Contains(output.outpoint)).ToList();
            return res;
        }

        private Dictionary<string, UnspentOutput> FetchUtxos(IEnumerable<Outpoint> outpoints)
        {
            var res = db.unspentOutputs
                        .Where(utxo => outpoints.Contains(utxo.Outpoint))
                        .ToDictionary(_ => _.Outpoint.ToString());

            return res;
        }
    }
}