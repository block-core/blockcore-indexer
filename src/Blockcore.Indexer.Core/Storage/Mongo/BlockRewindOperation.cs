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
   public static async Task RewindBlockOnIbdAsync(this MongoData storage, long blockIndex)
   {
      await DeleteBlockInCollectionFromTopOfTable(storage.UnspentOutputTable, nameof(UnspentOutputTable.BlockIndex),blockIndex);

      await RewindInputDataIntoUnspentTransactionTableAsync(storage, blockIndex);

      var output =
         DeleteBlockInCollectionFromTopOfTable(storage.OutputTable, nameof(OutputTable.BlockIndex),blockIndex);

      var input =
         DeleteBlockInCollectionFromTopOfTable(storage.InputTable, nameof(InputTable.BlockIndex),blockIndex);

      var transactions =
         DeleteBlockInCollectionFromTopOfTable(storage.TransactionBlockTable, nameof(TransactionBlockTable.BlockIndex),blockIndex);

      var addressComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressComputedTable, nameof(AddressComputedTable.ComputedBlockIndex),blockIndex);

      var addressHistoryComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressHistoryComputedTable, nameof(AddressHistoryComputedTable.BlockIndex),blockIndex);

      var addressUtxoComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressUtxoComputedTable, nameof(AddressUtxoComputedTable.BlockIndex),blockIndex);


      await Task.WhenAll(input, output, transactions, addressComputed, addressHistoryComputed, addressUtxoComputed);
   }

   public static async Task RewindBlockAsync(this MongoData storage, long blockIndex)
   {
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

      // delete computed utxo
      FilterDefinition<AddressUtxoComputedTable> addrCompUtxoFilter =
         Builders<AddressUtxoComputedTable>.Filter.Eq(addr => addr.BlockIndex, blockIndex);
      Task<DeleteResult> addressUtxoComputed = storage.AddressUtxoComputedTable.DeleteManyAsync(addrCompUtxoFilter);

      FilterDefinition<UnspentOutputTable> unspentOutputFilter =
         Builders<UnspentOutputTable>.Filter.Eq(utxo => utxo.BlockIndex, blockIndex);
      Task<DeleteResult> unspentOutput = storage.UnspentOutputTable.DeleteManyAsync(unspentOutputFilter);

      await Task.WhenAll(unspentOutput, output, transactions, addressComputed, addressHistoryComputed, addressUtxoComputed);

      await MergeRewindInputsToUnspentTransactionsAsync(storage,blockIndex);

      FilterDefinition<InputTable> inputFilter =
         Builders<InputTable>.Filter.Eq(addr => addr.BlockIndex, blockIndex);

      await storage.InputTable.DeleteManyAsync(inputFilter);
   }

   private static async Task RewindInputDataIntoUnspentTransactionTableAsync(MongoData storage, long blockIndex)
   {
      const int limit = 1000;
      int skip = 0;
      bool moreItemsToCopy;

      do
      {
         var lookupItems = await GetTopNDocumentsFromCollectionAsync<InputTable,InputTable>(storage.InputTable,limit * skip++, limit);

         var itemsToCopy = lookupItems
            .Where(_ => _.BlockIndex == blockIndex)
            .ToList();

         var outpoints = itemsToCopy.Select(_ => _.Outpoint).ToList();

         var existingOutpoint = (await storage.UnspentOutputTable
               .FindAsync(_ => outpoints.Contains(_.Outpoint)))
            .ToList();

         var filteredItemsToCopy = itemsToCopy
            .Where(_ => existingOutpoint.All(e => e.Outpoint != _.Outpoint))
            .Select(_ => new UnspentOutputTable
            {
               Address = _.Address, Outpoint = _.Outpoint, Value = _.Value, BlockIndex = -1
            })
            .ToList();

         if (!itemsToCopy.Any())
            break;

         await storage.UnspentOutputTable.InsertManyAsync(filteredItemsToCopy);
         moreItemsToCopy = itemsToCopy.Count == lookupItems.Count;


      } while (moreItemsToCopy );
   }

   private static async Task DeleteBlockInCollectionFromTopOfTable<T>(IMongoCollection<T> collection,
      string propertyName, long blockIndex)
   {
      const int limit = 10000;

      do
      {
         var lookupItems = await GetTopNDocumentsFromCollectionAsync<T,BsonDocument>(collection, 0,limit);

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

   private static Task MergeRewindInputsToUnspentTransactionsAsync(MongoData storage ,long blockIndex)
   {
      const string output = "Output";

      return storage.InputTable.Aggregate()
         .Match(_ => _.BlockIndex.Equals(blockIndex))
         .Lookup(storage.OutputTable.CollectionNamespace.CollectionName,
            new StringFieldDefinition<InputTable>(nameof(Outpoint)),
            new StringFieldDefinition<OutputTable>(nameof(Outpoint)),
            new StringFieldDefinition<BsonDocument>(output))
         .Unwind(_ => _[output])
         .Project(_ => new
         {
            //We need the block index that the output was created on, the rest of the data is the same as input
            Value = _[nameof(InputTable.Value)],
            Address = _[nameof(InputTable.Address)],
            BlockIndex = _[output][nameof(OutputTable.BlockIndex)],
            Outpoint = new
            {
               OutputIndex = _[nameof(Outpoint)][nameof(Outpoint.OutputIndex)],
               TransactionId = _[nameof(Outpoint)][nameof(Outpoint.OutputIndex)],
            }
         })
         .MergeAsync(storage.UnspentOutputTable);
   }

   private static async Task<List<TProjection>> GetTopNDocumentsFromCollectionAsync<T,TProjection>(IMongoCollection<T> collection,int skip, int limit)
   {
      return (await collection.FindAsync(FilterDefinition<T>.Empty,
               new FindOptions<T,TProjection>
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
