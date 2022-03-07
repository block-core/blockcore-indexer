using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public static class BlockRewindOperation
{
   public static async Task RewindBlockOnIbdAsync(this MongoDb storage, uint blockIndex)
   {
      await StoreRewindBlockAsync(storage, blockIndex);

      await RewindInputDataIntoUnspentTransactionTableAsync(storage, blockIndex);

      var unspent = DeleteBlockInCollectionFromTopOfTable(storage.UnspentOutputTable, nameof(UnspentOutputTable.BlockIndex),
         blockIndex);

      var output =
         DeleteBlockInCollectionFromTopOfTable(storage.OutputTable, nameof(OutputTable.BlockIndex), blockIndex);

      var input =
         DeleteBlockInCollectionFromTopOfTable(storage.InputTable, nameof(InputTable.BlockIndex), blockIndex);

      var transactions =
         DeleteBlockInCollectionFromTopOfTable(storage.TransactionBlockTable, nameof(TransactionBlockTable.BlockIndex),
            blockIndex);

      var addressComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressComputedTable,
            nameof(AddressComputedTable.ComputedBlockIndex), blockIndex);

      var addressHistoryComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressHistoryComputedTable,
            nameof(AddressHistoryComputedTable.BlockIndex), blockIndex);


      await Task.WhenAll(input, output, transactions, addressComputed, addressHistoryComputed, unspent);
   }

   public static async Task RewindBlockAsync(this MongoData storage, uint blockIndex)
   {
      await StoreRewindBlockAsync(storage, blockIndex);

      FilterDefinition<OutputTable> outputFilter =
         Builders<OutputTable>.Filter.Eq(addr => addr.BlockIndex, blockIndex);
      Task<DeleteResult> output = storage.OutputTable.DeleteManyAsync(outputFilter);

      // delete the transaction
      FilterDefinition<TransactionBlockTable> transactionFilter =
         Builders<TransactionBlockTable>.Filter.Eq(info => info.BlockIndex, blockIndex);
      Task<DeleteResult> transactions = storage.TransactionBlockTable.DeleteManyAsync(transactionFilter);

      // delete computed
      FilterDefinition<AddressComputedTable> addrCompFilter =
         Builders<AddressComputedTable>.Filter.Eq(addr => addr.ComputedBlockIndex, blockIndex);
      Task<DeleteResult> addressComputed = storage.AddressComputedTable.DeleteManyAsync(addrCompFilter);

      // delete computed history
      FilterDefinition<AddressHistoryComputedTable> addrCompHistFilter =
         Builders<AddressHistoryComputedTable>.Filter.Eq(addr => addr.BlockIndex, blockIndex);
      Task<DeleteResult> addressHistoryComputed = storage.AddressHistoryComputedTable.DeleteManyAsync(addrCompHistFilter);

      // this is an edge case, we delete from the utxo table in case a bath push failed half way and left
      // item in the utxo table that where suppose to get deleted, to avoid duplicates in recovery processes
      // we delete just in case (the utxo table has a unique key on outputs), there is no harm in deleting twice.
      FilterDefinition<UnspentOutputTable> unspentOutputFilter1 =
         Builders<UnspentOutputTable>.Filter.Eq(utxo => utxo.BlockIndex, blockIndex);
      Task<DeleteResult> unspentOutput1 = storage.UnspentOutputTable.DeleteManyAsync(unspentOutputFilter1);

      await Task.WhenAll( output, transactions, addressComputed, addressHistoryComputed, unspentOutput1);

      await MergeRewindInputsToUnspentTransactionsAsync(storage, blockIndex);

      FilterDefinition<InputTable> inputFilter =
         Builders<InputTable>.Filter.Eq(addr => addr.BlockIndex, blockIndex);

      Task<DeleteResult> inputs = storage.InputTable.DeleteManyAsync(inputFilter);

      // TODO: if we filtered out outputs that where created and spent as part of the same block
      // we may not need to delete again, however there is no harm in this extra delete.
      FilterDefinition<UnspentOutputTable> unspentOutputFilter =
         Builders<UnspentOutputTable>.Filter.Eq(utxo => utxo.BlockIndex, blockIndex);
      Task<DeleteResult> unspentOutput = storage.UnspentOutputTable.DeleteManyAsync(unspentOutputFilter);

      await Task.WhenAll( inputs, unspentOutput);
   }

   static Task StoreRewindBlockAsync(MongoDb storage, uint blockIndex)
   {
      var blockTask = storage.BlockTable.FindAsync(_ => _.BlockIndex == blockIndex);
      var inputsTask = storage.InputTable.FindAsync(_ => _.BlockIndex == blockIndex);
      var outputsTask = storage.OutputTable.FindAsync(_ => _.BlockIndex == blockIndex);
      var transactionIdsTask = storage.TransactionBlockTable.FindAsync(_ => _.BlockIndex == blockIndex);

      Task.WhenAll(blockTask, inputsTask, outputsTask, transactionIdsTask);

      BlockTable block = blockTask.Result.Single();

      var reorgBlock = new ReorgBlockTable
      {
         Created = System.DateTime.UtcNow,
         BlockIndex = blockIndex,
         BlockHash = block.BlockHash,
         Block = block,
         Inputs = inputsTask.Result.ToList(),
         Outputs = outputsTask.Result.ToList(),
         TransactionIds = transactionIdsTask.Result.ToList()
      };

      return storage.ReorgBlock.InsertOneAsync(reorgBlock);
   }

   private static async Task RewindInputDataIntoUnspentTransactionTableAsync(MongoDb storage, long blockIndex)
   {
      const int limit = 1000;
      int skip = 0;
      bool moreItemsToCopy;

      do
      {
         var lookupItems =
            await GetTopNDocumentsFromCollectionAsync<InputTable, InputTable>(storage.InputTable, limit * skip++,
               limit);

         var itemsToCopy = lookupItems
            .Where(_ => _.BlockIndex == blockIndex)
            .ToList();

         var outpoints = itemsToCopy.Select(_ => _.Outpoint).ToList();

         var existingOutpoint = (await storage.UnspentOutputTable
               .FindAsync(_ => outpoints.Contains(_.Outpoint)))
            .ToList();

         var filteredItemsToCopy = itemsToCopy
            .Where(_ => existingOutpoint.All(e => e.Outpoint.ToString() != _.Outpoint.ToString()))
            .Select(_ => new UnspentOutputTable
            {
               Address = _.Address, Outpoint = _.Outpoint, Value = _.Value, BlockIndex = int.MaxValue
            })
            .ToList();

         if (!filteredItemsToCopy.Any())
            break;

         await storage.UnspentOutputTable.InsertManyAsync(filteredItemsToCopy);
         moreItemsToCopy = itemsToCopy.Count == lookupItems.Count;


      } while (moreItemsToCopy);
   }

   private static async Task DeleteBlockInCollectionFromTopOfTable<T>(IMongoCollection<T> collection,
      string propertyName, long blockIndex)
   {
      const int limit = 10000;

      do
      {
         var lookupItems = await GetTopNDocumentsFromCollectionAsync<T, BsonDocument>(collection, 0, limit);

         if (!lookupItems.Any())
            break;

         foreach (BsonDocument bsonDocument in lookupItems)
         {
            if (bsonDocument[propertyName] != blockIndex)
               return;

            await collection.FindOneAndDeleteAsync(FilterDefinition<T>.Empty,
               new FindOneAndDeleteOptions<T>
               {
                  Sort = new BsonDocumentSortDefinition<T>(new BsonDocument("_id", -1))
               });
         }

      } while (true);
   }

   /// <summary>
   /// Inputs spend outputs, when an output is spent it gets deleted from the UnspendOutput table and the action of the delete is represented in the inputs table,
   /// when a rewind happens we need to bring back outputs that have been deleted from the UnspendOutput so we look for those outputs in the inputs table,
   /// however the block index in the inputs table is the one representing the input not the output we are trying to restore so we have to look it up in the outputs table.
   /// </summary>
   private static async Task MergeRewindInputsToUnspentTransactionsAsync(MongoData storage, long blockIndex)
   {
      List<UnspentOutputTable> unspentOutputs = await storage.InputTable.Aggregate<UnspentOutputTable>(
         new []
         {
            new BsonDocument("$match",
               new BsonDocument("BlockIndex", blockIndex)),
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Output" },
                  { "localField", "Outpoint" },
                  { "foreignField", "Outpoint" },
                  { "as", "Output" }
               }),
            new BsonDocument("$unwind",
               new BsonDocument("path", "$Output")),
            new BsonDocument("$project",
               new BsonDocument
               {
                  { "Value", "$Value" },
                  { "Address", "$Address" },
                  { "BlockIndex", "$Output.BlockIndex" },
                  { "Outpoint", "$Outpoint" }
               })
         }).ToListAsync();

      // this is to unsure the values are unique
      unspentOutputs.ToDictionary(a => a.Outpoint.ToString());

      // TODO: filter out any outputs that belong to the block being reorged.
      // this can happen for outputs that are created and spent in the same block.
      // if they get pushed now such outputs willjust get deleted in the next step.

      if (unspentOutputs.Any())
         await storage.UnspentOutputTable.InsertManyAsync(unspentOutputs);
   }

   private static async Task<List<TProjection>> GetTopNDocumentsFromCollectionAsync<T, TProjection>(
      IMongoCollection<T> collection, int skip, int limit)
   {
      return (await collection.FindAsync(FilterDefinition<T>.Empty,
               new FindOptions<T, TProjection>
               {
                  Sort = new BsonDocumentSortDefinition<T>(new BsonDocument("_id", -1)),
                  Limit = limit,
                  Skip = skip,
                  ShowRecordId = true
               },
               new CancellationToken(false))
            .ConfigureAwait(false))
         .ToList();
   }
}
