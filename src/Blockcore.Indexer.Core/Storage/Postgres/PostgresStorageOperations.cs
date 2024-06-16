
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Types;
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
            IUtxoCache utxoCache,
            IOptions<IndexerSettings> configuration,
            GlobalState globalState,
            IMapPgBlockToStorageBlock pgBlockToStorageBlock,
            IScriptInterpreter scriptInterpeter,
            PostgresDbContext context)
        {
            syncConnection = connection;
            // this.storage = storage;
            this.globalState = globalState;
            this.pgBlockToStorageBlock = pgBlockToStorageBlock;
            this.scriptInterpeter = scriptInterpeter;
            db = context;
            this.configuration = configuration.Value;
        }

        public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
        {
            storageBatch.TotalSize += item.BlockInfo.Size;
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
                    Output storageOutput = new Output
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
                    storageBatch.Outputs.Add(outpoint.ToString(), storageOutput);
                }

                if (trx.IsCoinBase)
                    continue;

                int inputIndex = 0;
                foreach (TxIn input in trx.Inputs)
                {
                    var outpoint = new Outpoint { Txid = input.PrevOut.Hash.ToString(), Vout = (int)input.PrevOut.N };
                    storageBatch.Outputs.TryGetValue(outpoint.ToString(), out Output output);

                    Input storageInput = new Input
                    {
                        outpoint = outpoint,
                        Txid = txid,
                        BlockIndex = item.BlockInfo.HeightAsUint32,
                        Value = output?.Value ?? 0,
                    };
                    transaction.Inputs.Add(storageInput);
                    storageBatch.Inputs.Add(storageInput);
                }

                block.Transactions.Add(transaction);
                block.TransactionCount++;
            }
            OnAddtoStorageBatch(storageBatch, item);
            storageBatch.Blocks.Add(item.BlockInfo.Height, pgBlockToStorageBlock.Map(item.BlockInfo));
        }

        private void OnAddtoStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item) => throw new NotImplementedException();
        public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item) => throw new System.NotImplementedException();
        public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
        {
            if (globalState.IndexModeCompleted)
            {
                if (globalState.IbdMode() == false)
                {
                    if (globalState.LocalMempoolView.Any())
                    {
                        var toRemoveFromMempool = storageBatch.Blocks.Values.SelectMany(b => b.Transactions.Select(t => t.Txid));
                        Task deleteFromMempoolTask = Task.Run(async () =>
                        {
                            await db.mempoolTransactions.Where(mt => toRemoveFromMempool.Contains(mt.Txid)).ExecuteDeleteAsync();
                        });
                        // db.BulkDelete(storageBatch.Blocks.Values.SelectMany(b => b.Transactions));

                        deleteFromMempoolTask.Wait();

                        foreach (string mempooltrx in toRemoveFromMempool)
                        {
                            globalState.LocalMempoolView.Remove(mempooltrx, out _);
                        }
                    }
                }
            }


            var utxos = new List<UnspentOutput>(storageBatch.Outputs.Values.Count);

            foreach (Output output in storageBatch.Outputs.Values)
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

            if (storageBatch.Inputs.Any())
            {
                var utxoLookups = FetchUtxos(storageBatch.Inputs
                    .Where(_ => _.Address == null)
                    .Select(_ => _.outpoint));

                foreach (Input input in storageBatch.Inputs)
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
                await db.BulkInsertAsync(storageBatch.Blocks.Values, options => { options.IncludeGraph = true; });
            });

            Task.WaitAll(pushBatchToPostgresTask, utxoInsertTask);

            if (storageBatch.Inputs.Any())
            {
                // TODO: if earlier we filtered out outputs that are already spent and not pushed to the utxo table
                // now we do not need to try and delete such outputs becuase they where never pushed to the store.
                var outpointsFromNewInput = storageBatch.Inputs.Select(_ => _.outpoint).ToList();

                int rowsDeleted = db.unspentOutputs.Where(uo => outpointsFromNewInput.Contains(uo.Outpoint)).ExecuteDelete();

                if (rowsDeleted != outpointsFromNewInput.Count)
                {
                    throw new ApplicationException($"Delete of unspent outputs did not complete successfully : {rowsDeleted} deleted but {outpointsFromNewInput.Count} expected");
                }
            }

            OnPushStorageBatch(storageBatch);

            string lastBlockHash = null;
            long blockIndex = 0;
            List<Block> markBlocksAsComplete = [];
            foreach (Block mapBlock in storageBatch.Blocks.Values.OrderBy(b => b.BlockIndex))
            {
                mapBlock.SyncComplete = true;
                markBlocksAsComplete.Add(mapBlock);
                lastBlockHash = mapBlock.BlockHash;
                blockIndex = mapBlock.BlockIndex;
            }

            db.BulkUpdate(markBlocksAsComplete);

            SyncBlockInfo block = storage.BlockByIndex(blockIndex);

            if (block.BlockHash != lastBlockHash)
            {
                throw new ArgumentException($"Expected hash {blockIndex} for block {lastBlockHash} but was {block.BlockHash}");
            }

            return block;
        }

        private void OnPushStorageBatch(StorageBatch storageBatch) => throw new NotImplementedException();

        private Dictionary<string, UnspentOutput> FetchUtxos(IEnumerable<Outpoint> outputs)
        {
            var outpoints = outputs.Select(o => o.ToString()).ToList();

            var res = db.unspentOutputs
                        .Where(utxo => outpoints.Contains(utxo.Outpoint.ToString()))
                        .ToDictionary(_ => _.Outpoint.ToString());

            return res;
        }
    }
}