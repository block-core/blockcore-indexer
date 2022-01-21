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
      await DeleteBlockInCollectionFromTopOfTable(storage.UnspentOutputTable, _ => _.BlockIndex == blockIndex);

      await RewindInputDataIntoUnspentTransactionTableAsync(storage, blockIndex);

      var output =
         DeleteBlockInCollectionFromTopOfTable(storage.OutputTable, _ => _.BlockIndex == blockIndex);

      var input =
         DeleteBlockInCollectionFromTopOfTable(storage.InputTable, _ => _.BlockIndex == blockIndex);

      var transactions =
         DeleteBlockInCollectionFromTopOfTable(storage.TransactionBlockTable, _ => _.BlockIndex == blockIndex);

      var addressComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressComputedTable, _ => _.ComputedBlockIndex == blockIndex);

      var addressHistoryComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressHistoryComputedTable, _ => _.BlockIndex == blockIndex);

      var addressUtxoComputed =
         DeleteBlockInCollectionFromTopOfTable(storage.AddressUtxoComputedTable, _ => _.BlockIndex == blockIndex);


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

      bool moreItemsToCopy;

      do
      {
         var lookupItems = await GetTopNDocumentsFromCollectionAsync(storage.InputTable, limit);

         var itemsToCopy = lookupItems
            .Where(_ => _.BlockIndex == blockIndex)
            .Select(_ => new UnspentOutputTable
            {
               Address = _.Address,Outpoint = _.Outpoint,Value = _.Value,BlockIndex = -1
            })
            .ToList();

         await storage.UnspentOutputTable.InsertManyAsync(itemsToCopy);
         moreItemsToCopy = itemsToCopy.Count == lookupItems.Count;

      } while (moreItemsToCopy );
   }

   private static async Task DeleteBlockInCollectionFromTopOfTable<T>(IMongoCollection<T> collection,
      Func<T,bool> whereFilter)
   {
      const int limit = 1000;

      bool moreItemsToDelete;

      do
      {
         var lookupItems = await GetTopNDocumentsFromCollectionAsync(collection, limit);

         var itemsToDelete = lookupItems
            .Where(whereFilter)
            .ToList();

         var filterToDelete = Builders<T>.Filter
            .Where(_ => itemsToDelete.Contains(_));

         await collection.DeleteManyAsync(filterToDelete);
         moreItemsToDelete = itemsToDelete.Count == lookupItems.Count;

      } while (moreItemsToDelete );
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

   private static async Task<List<T>> GetTopNDocumentsFromCollectionAsync<T>(IMongoCollection<T> collection, int limit)
   {
      return (await collection.FindAsync(FilterDefinition<T>.Empty,
               new FindOptions<T>
               {
                  Sort = new BsonDocumentSortDefinition<T>(new BsonDocument("_id", -1)), Limit = limit
               },
               new CancellationToken(false))
            .ConfigureAwait(false))
         .ToList();
   }
}
