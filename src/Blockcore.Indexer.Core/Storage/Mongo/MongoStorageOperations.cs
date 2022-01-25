using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NBitcoin;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class MongoStorageOperations : IStorageOperations
   {
      protected readonly SyncConnection syncConnection;
      protected readonly GlobalState globalState;
      protected readonly IScriptInterpeter scriptInterpeter;
      protected readonly IndexerSettings configuration;
      protected readonly MongoData data;
      protected readonly IMapMongoBlockToStorageBlock mongoBlockToStorageBlock;

      public MongoStorageOperations(
         SyncConnection syncConnection,
         IStorage storage,
         IUtxoCache utxoCache,
         IOptions<IndexerSettings> configuration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         IScriptInterpeter scriptInterpeter)
      {
         this.syncConnection = syncConnection;
         this.globalState = globalState;
         this.scriptInterpeter = scriptInterpeter;
         this.mongoBlockToStorageBlock = mongoBlockToStorageBlock;
         this.configuration = configuration.Value;
         data = (MongoData)storage;
      }

      public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         storageBatch.TotalSize += item.BlockInfo.Size;
         storageBatch.BlockTable.Add(item.BlockInfo.Height, mongoBlockToStorageBlock.Map(item.BlockInfo));

         storageBatch.TransactionBlockTable.AddRange(item.Transactions.Select(s => new TransactionBlockTable
         {
            BlockIndex = item.BlockInfo.Height, TransactionId = s.GetHash().ToString()
         }));

         if (configuration.StoreRawTransactions)
         {
            storageBatch.TransactionTable.AddRange(item.Transactions.Select(t => new TransactionTable
            {
               TransactionId = t.GetHash().ToString(),
               RawTransaction = t.ToBytes(syncConnection.Network.Consensus.ConsensusFactory)
            }));
         }

         item.Transactions.ForEach((trx, i) =>
            trx.Outputs.ForEach((output, index) =>
            {
               ScriptOutputInfo res = scriptInterpeter.InterpretScript(syncConnection.Network, output.ScriptPubKey);
               string addr = res != null
                  ? (res?.Addresses != null && res.Addresses.Any()) ? res.Addresses.First() : res.ScriptType.ToString()
                  : "none";

               var outpoint = new Outpoint { TransactionId = trx.GetHash().ToString(), OutputIndex = index };

               //TODO David need to change AddOrReplace to fit the BTC issue with repeating outputs that was fixed with hard-coding the 2 transactions that had that
               storageBatch.OutputTable.AddOrReplace(outpoint.ToString(), new OutputTable
               {
                  Address = addr,
                  Outpoint = outpoint,
                  BlockIndex = item.BlockInfo.Height,
                  Value = output.Value,
                  ScriptHex = output.ScriptPubKey.ToHex(),
                  CoinBase = trx.IsCoinBase,
                  CoinStake = syncConnection.Network.Consensus.IsProofOfStake && trx.IsCoinStake,
               });
            }));


         item.Transactions
            .Where(t => t.IsCoinBase == false)
            .ForEach((trx, trxIndex) =>
               trx.Inputs.ForEach((input, inputIndex) =>
               {
                  var outpoint = new Outpoint
                  {
                     TransactionId = input.PrevOut.Hash.ToString(), OutputIndex = (int)input.PrevOut.N
                  };

                  storageBatch.OutputTable.TryGetValue(outpoint.ToString(), out OutputTable output);

                  storageBatch.InputTable.Add(new InputTable()
                  {
                     Outpoint = outpoint,
                     TrxHash = trx.GetHash().ToString(),
                     BlockIndex = item.BlockInfo.Height,
                     Address = output?.Address,
                     Value = output?.Value ?? 0,
                  });
               }));


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

                  data.Mempool.DeleteMany(filter);

                  foreach (string mempooltrx in toRemoveFromMempool)
                     globalState.LocalMempoolView.Remove(mempooltrx, out _);
               }
            }
         }

         var blockTableTask = storageBatch.BlockTable.Values.Any()
            ? data.BlockTable.InsertManyAsync(storageBatch.BlockTable.Values,
               new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;

         var transactionBlockTableTask = storageBatch.TransactionBlockTable.Any()
            ? data.TransactionBlockTable.InsertManyAsync(storageBatch.TransactionBlockTable,
               new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;

         var outputTableTask = storageBatch.OutputTable.Any()
            ? data.OutputTable.InsertManyAsync(storageBatch.OutputTable.Values,
               new InsertManyOptions { IsOrdered = false })
            : Task.CompletedTask;


         var inputTableTask = Task.CompletedTask;
         if (storageBatch.InputTable.Any())
         {
            var utxosLookups = FetchUtxos(storageBatch.InputTable
               .Where(_ => _.Address == null)
               .Select(_ => _.Outpoint));

            foreach (InputTable input in storageBatch.InputTable)
            {
               if (input.Address != null) continue;

               string key = input.Outpoint.ToString();
               input.Address = utxosLookups[key].Address;
               input.Value = utxosLookups[key].Value;
            }

            inputTableTask = data.InputTable.InsertManyAsync(storageBatch.InputTable, new InsertManyOptions { IsOrdered = false });
         }

         Task transactionTableTask = Task.CompletedTask;

         try
         {
            if (storageBatch.TransactionTable.Any())
            {
               transactionTableTask = data.TransactionTable.InsertManyAsync(storageBatch.TransactionTable,
                  new InsertManyOptions { IsOrdered = false });
            }
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

         var utxos = new List<UnspentOutputTable>(storageBatch.OutputTable.Values.Count);

         foreach (OutputTable outputTable in storageBatch.OutputTable.Values)
         {
            utxos.Add(new UnspentOutputTable
            {
               Address = outputTable.Address,
               Outpoint = outputTable.Outpoint,
               Value = outputTable.Value,
               BlockIndex = outputTable.BlockIndex
            });
         }

         var unspentOutputTableTask = utxos.Any()
            ? data.UnspentOutputTable.InsertManyAsync(utxos)
            : Task.CompletedTask;

         Task.WaitAll(blockTableTask, transactionBlockTableTask, outputTableTask, inputTableTask, transactionTableTask, unspentOutputTableTask);

         var outpointsFromNewInput = storageBatch.InputTable
            .Select(_ => _.Outpoint)
            .ToList();

         if (outpointsFromNewInput.Any())
         {
            var filterToDelete = Builders<UnspentOutputTable>.Filter
               .Where(_ => outpointsFromNewInput.Contains(_.Outpoint));

            data.UnspentOutputTable.DeleteMany(filterToDelete);
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
         data.BlockTable.BulkWrite(markBlocksAsComplete, new BulkWriteOptions() { IsOrdered = true });

         SyncBlockInfo block = data.BlockByIndex(blockIndex);

         if (block.BlockHash != lastBlockHash)
         {
            throw new ArgumentException(
               $"Expected hash {lastBlockHash} for block {blockIndex} but was {block.BlockHash}");
         }

         return block;
      }

      public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item)
      {
         var mempool = new List<MempoolTable>();
         var inputs = new Dictionary<string, (MempoolInput mempoolInput, MempoolTable mempool)>();

         foreach (Transaction itemTransaction in item.Transactions)
         {
            var mempoolEntry = new MempoolTable() { TransactionId = itemTransaction.GetHash().ToString() };
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
            data.Mempool.InsertMany(mempool, new InsertManyOptions { IsOrdered = false });
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

         var res = data.OutputTable.Find(filter).ToList();

         return res;
      }

      private Dictionary<string,UnspentOutputTable> FetchUtxos(IEnumerable<Outpoint> outputs)
      {
         FilterDefinitionBuilder<UnspentOutputTable> builder = Builders<UnspentOutputTable>.Filter;
         FilterDefinition<UnspentOutputTable> filter = builder.In(utxo => utxo.Outpoint, outputs);

         var res = data.UnspentOutputTable.FindSync(filter)
            .ToList()
            .ToDictionary(_ => _.Outpoint.ToString());

         return res;
      }
   }
}
