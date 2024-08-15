using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.NBitcoin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Transaction = Blockcore.Indexer.Core.Storage.Postgres.Types.Transaction;
using Output = Blockcore.Indexer.Core.Storage.Postgres.Types.Output;
using Input = Blockcore.Indexer.Core.Storage.Postgres.Types.Input;



namespace Blockcore.Indexer.Core.Storage.Postgres
{
    public class PostgresStorageOperations : IStorageOperations
    {
        const string OpReturnAddress = "TX_NULL_DATA";
        protected readonly SyncConnection syncConnection;
        protected readonly GlobalState globalState;
        protected readonly IScriptInterpreter scriptInterpeter;
        protected readonly IndexerSettings configuration;
        protected readonly IDbContextFactory<PostgresDbContext> contextFactory;
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
            IDbContextFactory<PostgresDbContext> contextFactory)
        {
            syncConnection = connection;
            this.storage = storage;
            this.globalState = globalState;
            this.pgBlockToStorageBlock = pgBlockToStorageBlock;
            this.scriptInterpeter = scriptInterpeter;
            this.contextFactory = contextFactory;
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

                    var outpoint = new Outpoint { TransactionId = txid, OutputIndex = outputIndex++ };
                    var storageOutput = new Output
                    {
                        Outpoint = outpoint,
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


                foreach (TxIn input in trx.Inputs)
                {
                    var outpoint = new Outpoint { TransactionId = input.PrevOut.Hash.ToString(), OutputIndex = (int)input.PrevOut.N };
                    postgresStorageBatch.Outputs.TryGetValue(outpoint.ToString(), out Output output);

                    Input storageInput = new Input
                    {
                        Outpoint = outpoint,
                        TrxHash = txid,
                        BlockIndex = item.BlockInfo.HeightAsUint32,
                        Address = output?.Address,
                        Value = output?.Value ?? 0,
                    };
                    transaction.Inputs.Add(storageInput);
                    postgresStorageBatch.Inputs.Add(storageInput);
                }

                block.Transactions.Add(transaction);
                block.TransactionCount++;
            }
            OnAddtoStorageBatch(postgresStorageBatch, item);
            postgresStorageBatch.Blocks.Add(item.BlockInfo.Height, block);
        }

        public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
        {
            // using PostgresDbContext db = contextFactory.CreateDbContext();

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
                            await contextFactory.CreateDbContext().mempoolTransactions.Where(mt => toRemoveFromMempool.Contains(mt.TransactionId)).ExecuteDeleteAsync();
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
                    Outpoint = output.Outpoint,
                    Value = output.Value,
                    BlockIndex = output.BlockIndex,
                });
            }

            Task utxoInsertTask = utxos.Any() ? Task.Run(async () =>
                await contextFactory.CreateDbContext().BulkInsertAsync(utxos))
                : Task.CompletedTask;

            if (postgresStorageBatch.Inputs.Any())
            {
                var utxoLookups = FetchUtxos(postgresStorageBatch.Inputs
                    .Where(_ => _.Address == null)
                    .Select(_ => _.Outpoint.TransactionId).ToList(),
                    postgresStorageBatch.Inputs.Where(_ => _.Address == null).Select(_ => _.Outpoint.OutputIndex).ToList() );

                foreach (Input input in postgresStorageBatch.Inputs)
                {
                    if (input.Address != null)
                    {
                        continue;
                    }

                    string key = input.Outpoint.ToString();
                    input.Address = utxoLookups[key].Address;
                    input.Value = utxoLookups[key].Value;
                }
            }

            Task pushBatchToPostgresTask = Task.Run(async () =>
            {
                using var db = contextFactory.CreateDbContext();
                await db.BulkInsertAsync(postgresStorageBatch.Blocks.Values, options => { options.IncludeGraph = true; options.InsertKeepIdentity = true; });
            });

            Task.WaitAll(pushBatchToPostgresTask, utxoInsertTask);

            if (postgresStorageBatch.Inputs.Any())
            {
                // TODO: if earlier we filtered out outputs that are already spent and not pushed to the utxo table
                // now we do not need to try and delete such outputs becuase they where never pushed to the store.
                var outpointsFromNewInput = postgresStorageBatch.Inputs.Select(_ => _.Outpoint.ToString()).ToList();

                int rowsDeleted = contextFactory.CreateDbContext().unspentOutputs.Where(uo => outpointsFromNewInput.Contains(uo.Outpoint.TransactionId + "-" + uo.Outpoint.OutputIndex)).ExecuteDelete();

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

            contextFactory.CreateDbContext().BulkUpdate(markBlocksAsComplete);

            SyncBlockInfo block = storage.BlockByIndex(blockIndex);

            if (block.BlockHash != lastBlockHash)
            {
                throw new ArgumentException($"Expected hash {blockIndex} for block {lastBlockHash} but was {block.BlockHash}");
            }

            return block;
        }

        public void InsertMempoolTransactions(SyncBlockTransactionsOperation item)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();

            var mempool = new List<MempoolTransaction>();
            var inputs = new Dictionary<string, (MempoolInput mempoolInput, MempoolTransaction mempool)>();

            foreach (var itemTransaction in item.Transactions)
            {
                var mempoolEntry = new MempoolTransaction() { TransactionId = itemTransaction.GetHash().ToString(), FirstSeen = DateTime.UtcNow.Ticks };
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
                        Outpoint = new Outpoint
                        {
                            OutputIndex = (int)transactionInput.PrevOut.N,
                            TransactionId = transactionInput.PrevOut.Hash.ToString()
                        }
                    };
                    mempoolEntry.Inputs.Add(input);
                    inputs.Add($"{input.Outpoint.TransactionId}-{input.Outpoint.OutputIndex}", (input, mempoolEntry));
                }
            }

            List<Output> outputsFromStore = FetchOutputs(inputs.Values.Select(s => s.mempoolInput.Outpoint).ToList());

            foreach (Output outputFromStore in outputsFromStore)
            {
                if (inputs.TryGetValue($"{outputFromStore.Outpoint.TransactionId}-{outputFromStore.Outpoint.OutputIndex}",
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
            Task insertToMempoolTask = Task.Run(async () => await db.BulkInsertAsync(mempool, bulkConfig => bulkConfig.IncludeGraph = true));//, options => options.IncludeGraph = true));
            try
            {
                Task.WaitAll(insertToMempoolTask);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            foreach (MempoolTransaction mempooltrx in mempool)
                globalState.LocalMempoolView.TryAdd(mempooltrx.TransactionId, string.Empty);
        }
        protected virtual void OnAddtoStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
        {

        }
        private void OnPushStorageBatch(StorageBatch storageBatch) { }
        private List<Output> FetchOutputs(List<Outpoint> outpoints)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();

            var res = db.Outputs.Where(output => outpoints.Contains(output.Outpoint)).ToList();
            return res;
        }

        private Dictionary<string, UnspentOutput> FetchUtxos(List<string> outpoint_txid, List<int> outpoint_vout)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();

            var res = db.unspentOutputs
                        .Where(utxo => outpoint_txid.Contains(utxo.Outpoint.TransactionId))
                        .Where(utxo => outpoint_vout.Contains(utxo.Outpoint.OutputIndex))
                        .ToDictionary(utxo => utxo.Outpoint.ToString(), utxo => utxo);

            return res;
        }
    }
}
