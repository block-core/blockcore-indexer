using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.NBitcoin;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class MongoStorageOperations : IStorageOperations
   {
      const string OpReturnAddress = "TX_NULL_DATA";

      protected readonly SyncConnection syncConnection;
      protected readonly GlobalState globalState;
      protected readonly IScriptInterpreter scriptInterpeter;
      protected readonly IndexerSettings configuration;
      protected readonly IMongoDb db;
      protected readonly IStorage storage;
      protected readonly IMapMongoBlockToStorageBlock mongoBlockToStorageBlock;

      public MongoStorageOperations(
         SyncConnection syncConnection,
         IMongoDb storage,
         IUtxoCache utxoCache,
         IOptions<IndexerSettings> configuration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         IScriptInterpreter scriptInterpeter,
         IStorage storage1)
      {
         this.syncConnection = syncConnection;
         this.globalState = globalState;
         this.scriptInterpeter = scriptInterpeter;
         this.storage = storage1;
         this.mongoBlockToStorageBlock = mongoBlockToStorageBlock;
         this.configuration = configuration.Value;
         db = storage;
      }

      public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         storageBatch.TotalSize += item.BlockInfo.Size;
         storageBatch.BlockTable.Add(item.BlockInfo.Height, mongoBlockToStorageBlock.Map(item.BlockInfo));

         int transactionIndex = 0;
         foreach (Transaction trx in item.Transactions)
         {
            string trxHash = trx.GetHash().ToString();

            storageBatch.TransactionBlockTable.Add(
               new TransactionBlockTable
               {
                  BlockIndex = item.BlockInfo.HeightAsUint32,
                  TransactionId = trxHash,
                  TransactionIndex = transactionIndex++,
                  NumberOfOutputs = (short) trx.Outputs.Count
               });

            if (configuration.StoreRawTransactions)
            {
               storageBatch.TransactionTable.Add(new TransactionTable
               {
                  TransactionId = trxHash,
                  RawTransaction = trx.ToBytes(syncConnection.Network.Consensus.ConsensusFactory)
               });
            }

            int outputIndex = 0;
            foreach (TxOut output in trx.Outputs)
            {
               ScriptOutputInfo res = scriptInterpeter.InterpretScript(syncConnection.Network, output.ScriptPubKey);
               string addr = res != null
                  ? (res?.Addresses != null && res.Addresses.Any()) ? res.Addresses.First() : res.ScriptType
                  : "none";

               var outpoint = new Outpoint { TransactionId = trxHash, OutputIndex = outputIndex++ };

               storageBatch.OutputTable.Add(outpoint.ToString(), new OutputTable
               {
                  Address = addr,
                  Outpoint = outpoint,
                  BlockIndex = item.BlockInfo.HeightAsUint32,
                  Value = output.Value,
                  ScriptHex = output.ScriptPubKey.ToHex(),
                  CoinBase = trx.IsCoinBase,
                  CoinStake = syncConnection.Network.Consensus.IsProofOfStake && trx.IsCoinStake,
               });
            }

            if (trx.IsCoinBase)
               continue; //no need to check the inputs for that transaction

            foreach (TxIn input in trx.Inputs)
            {
               var outpoint = new Outpoint
               {
                  TransactionId = input.PrevOut.Hash.ToString(), OutputIndex = (int)input.PrevOut.N
               };

               storageBatch.OutputTable.TryGetValue(outpoint.ToString(), out OutputTable output);

               storageBatch.InputTable.Add(new InputTable()
               {
                  Outpoint = outpoint,
                  TrxHash = trxHash,
                  BlockIndex = item.BlockInfo.HeightAsUint32,
                  Address = output?.Address,
                  Value = output?.Value ?? 0,
               });
            }

         }

         // allow any extensions to add ot the batch.
         OnAddToStorageBatch(storageBatch, item);
      }

      public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
      {
         if (globalState.IndexModeCompleted)
         {
            if (globalState.IbdMode() == false)
            {
               if (globalState.LocalMempoolView.Any())
               {
                  var toRemoveFromMempool = storageBatch.TransactionBlockTable.Select(s => s.TransactionId).ToList();

                  FilterDefinitionBuilder<MempoolTable> builder = Builders<MempoolTable>.Filter;
                  FilterDefinition<MempoolTable> filter = builder.In(mempoolItem => mempoolItem.TransactionId,
                     toRemoveFromMempool);

                  db.Mempool.DeleteMany(filter);

                  foreach (string mempooltrx in toRemoveFromMempool)
                     globalState.LocalMempoolView.Remove(mempooltrx, out _);
               }
            }
         }

         var blockTableTask = storageBatch.BlockTable.Values.Any()
            ? db.BlockTable.InsertManyAsync(storageBatch.BlockTable.Values, new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;

         var transactionBlockTableTask = storageBatch.TransactionBlockTable.Any()
            ? db.TransactionBlockTable.InsertManyAsync(storageBatch.TransactionBlockTable, new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;

         var outputTableTask = storageBatch.OutputTable.Any()
            ? db.OutputTable.InsertManyAsync(storageBatch.OutputTable.Values, new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;

         Task transactionTableTask = Task.Run(() =>
         {
            try
            {
               if (storageBatch.TransactionTable.Any())
                  db.TransactionTable.InsertMany(storageBatch.TransactionTable, new InsertManyOptions {IsOrdered = false});
            }
            catch (MongoBulkWriteException mbwex)
            {
               // transactions are a special case they are not deleted from store in case of reorgs
               // because they will just be included in another blocks, so we ignore if key is already present
               if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))
               {
                  throw;
               }
            }
         });

         var utxos = new List<UnspentOutputTable>(storageBatch.OutputTable.Values.Count);

         foreach (OutputTable outputTable in storageBatch.OutputTable.Values)
         {
            if (outputTable.Address.Equals(OpReturnAddress))
               continue;

            // TODO: filter out outputs that are already spent in the storageBatch.InputTable table
            // such inputs will get deleted anyway in the next operation of UnspentOutputTable.DeleteMany
            // this means we should probably make the storageBatch.InputTable a dictionary as well.

            utxos.Add(new UnspentOutputTable
            {
               Address = outputTable.Address,
               Outpoint = outputTable.Outpoint,
               Value = outputTable.Value,
               BlockIndex = outputTable.BlockIndex
            });
         }

         var unspentOutputTableTask = utxos.Any()
            ? db.UnspentOutputTable.InsertManyAsync(utxos, new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;

         var inputTableTask = Task.CompletedTask;
         if (storageBatch.InputTable.Any())
         {
            var utxosLookups = FetchUtxos(
               storageBatch.InputTable
                  .Where(_ => _.Address == null)
                  .Select(_ => _.Outpoint));

            foreach (InputTable input in storageBatch.InputTable)
            {
               if (input.Address != null) continue;

               string key = input.Outpoint.ToString();
               input.Address = utxosLookups[key].Address;
               input.Value = utxosLookups[key].Value;
            }

            inputTableTask = db.InputTable.InsertManyAsync(storageBatch.InputTable, new InsertManyOptions { IsOrdered = false });
         }

         Task.WaitAll(blockTableTask, transactionBlockTableTask, outputTableTask, inputTableTask, transactionTableTask, unspentOutputTableTask);

         if (storageBatch.InputTable.Any())
         {
            // TODO: if earlier we filtered out outputs that are already spent and not pushed to the utxo table
            // now we do not need to try and delete such outputs becuase they where never pushed to the store.

            var outpointsFromNewInput = storageBatch.InputTable
            .Select(_ => _.Outpoint)
            .ToList();

            var filterToDelete = Builders<UnspentOutputTable>.Filter
               .Where(_ => outpointsFromNewInput.Contains(_.Outpoint));

            var deleteResult = db.UnspentOutputTable.DeleteMany(filterToDelete);

            if (deleteResult.DeletedCount != outpointsFromNewInput.Count)
               throw new ApplicationException($"Delete of unspent outputs did not complete successfully : {deleteResult.DeletedCount} deleted but {outpointsFromNewInput.Count} expected");
         }

         // allow any extensions to push to repo before we complete the block.
         OnPushStorageBatch(storageBatch);

         string lastBlockHash = null;
         long blockIndex = 0;
         var markBlocksAsComplete = new List<UpdateOneModel<BlockTable>>();
         foreach (BlockTable mapBlock in storageBatch.BlockTable.Values.OrderBy(b => b.BlockIndex))
         {
            FilterDefinition<BlockTable> filter =
               Builders<BlockTable>.Filter.Eq(block => block.BlockIndex, mapBlock.BlockIndex);
            UpdateDefinition<BlockTable> update =
               Builders<BlockTable>.Update.Set(blockInfo => blockInfo.SyncComplete, true);

            markBlocksAsComplete.Add(new UpdateOneModel<BlockTable>(filter, update));
            lastBlockHash = mapBlock.BlockHash;
            blockIndex = mapBlock.BlockIndex;
         }

         // mark each block is complete
         db.BlockTable.BulkWrite(markBlocksAsComplete, new BulkWriteOptions() { IsOrdered = true });

         SyncBlockInfo block = storage.BlockByIndex(blockIndex);

         if (block.BlockHash != lastBlockHash)
         {
            throw new ArgumentException($"Expected hash {blockIndex} for block {lastBlockHash} but was {block.BlockHash}");
         }

         return block;
      }

      public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item)
      {
         var mempool = new List<MempoolTable>();
         var inputs = new Dictionary<string, (MempoolInput mempoolInput, MempoolTable mempool)>();

         foreach (Transaction itemTransaction in item.Transactions)
         {
            var mempoolEntry = new MempoolTable() { TransactionId = itemTransaction.GetHash().ToString(), FirstSeen = DateTime.UtcNow.Ticks };
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

         List<OutputTable> outputsFromStore = FetchOutputs(inputs.Values.Select(s => s.mempoolInput.Outpoint).ToList());

         foreach (OutputTable outputFromStore in outputsFromStore)
         {
            if (inputs.TryGetValue($"{outputFromStore.Outpoint.TransactionId}-{outputFromStore.Outpoint.OutputIndex}",
                   out (MempoolInput mempoolInput, MempoolTable mempool) input))
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

         try
         {
            db.Mempool.InsertMany(mempool, new InsertManyOptions { IsOrdered = false });
         }
         catch (MongoBulkWriteException mbwex)
         {
            // if a mempool trx already exists in mempool ignore it
            if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))
            {
               throw;
            }
         }

         foreach (MempoolTable mempooltrx in mempool)
            globalState.LocalMempoolView.TryAdd(mempooltrx.TransactionId, string.Empty);

         return new InsertStats { Items = mempool };
      }

      protected virtual void OnAddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {

      }

      protected virtual void OnPushStorageBatch(StorageBatch storageBatch)
      {

      }

      private List<OutputTable> FetchOutputs(List<Outpoint> outputs)
      {
         FilterDefinitionBuilder<OutputTable> builder = Builders<OutputTable>.Filter;
         FilterDefinition<OutputTable> filter = builder.In(output => output.Outpoint, outputs);

         var res = db.OutputTable.Find(filter).ToList();

         return res;
      }

      private Dictionary<string,UnspentOutputTable> FetchUtxos(IEnumerable<Outpoint> outputs)
      {
         FilterDefinitionBuilder<UnspentOutputTable> builder = Builders<UnspentOutputTable>.Filter;
         FilterDefinition<UnspentOutputTable> filter = builder.In(utxo => utxo.Outpoint, outputs);

         var res = db.UnspentOutputTable.FindSync(filter)
            .ToList()
            .ToDictionary(_ => _.Outpoint.ToString());

         return res;
      }
   }
}
