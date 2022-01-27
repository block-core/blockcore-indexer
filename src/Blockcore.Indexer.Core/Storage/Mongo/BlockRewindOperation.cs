using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public static class BlockRewindOperation
{
   public static Task RewindBlockOnIbdAsync(this MongoData storage, long blockIndex)
   {
      //We have too many scenarios that add complexity to the sync (and really slows it down) with very little benefit as this is really a problem with the
      //infrastructure running (mongodb, node etc.) and not an actual rewind in the block chain

      throw new ApplicationException("The sync has failed in IBD and should be started from genesis");
   }

   public static async Task RewindBlockAsync(this MongoData storage, long blockIndex)
   {
      Task<DeleteResult> output =  DeleteDocumentsFromDbByBlockIndexAsync(storage.OutputTable, _ => _.BlockIndex, blockIndex);
      Task<DeleteResult> transactions =  DeleteDocumentsFromDbByBlockIndexAsync(storage.TransactionBlockTable, _ => _.BlockIndex, blockIndex);
      Task<DeleteResult> addressComputed =  DeleteDocumentsFromDbByBlockIndexAsync(storage.AddressComputedTable, _ => _.ComputedBlockIndex, blockIndex);
      Task<DeleteResult> addressHistoryComputed =  DeleteDocumentsFromDbByBlockIndexAsync(storage.AddressHistoryComputedTable, _ => _.BlockIndex, blockIndex);
      Task<DeleteResult> unspentOutput =  DeleteDocumentsFromDbByBlockIndexAsync(storage.UnspentOutputTable, _ => _.BlockIndex, blockIndex);

      await MergeRewindInputsToUnspentTransactionsAsync(storage, blockIndex);

      await  DeleteDocumentsFromDbByBlockIndexAsync(storage.InputTable, _ => _.BlockIndex, blockIndex);
   }

   private static Task<DeleteResult> DeleteDocumentsFromDbByBlockIndexAsync<T>(IMongoCollection<T> collection,
      Expression<Func<T, long>> field, long value)
   {
      FilterDefinition<T> unspentOutputFilter = Builders<T>.Filter.Eq(field, value);
      return collection.DeleteManyAsync(unspentOutputFilter);
   }

   /// <summary>
   /// Inputs spend outputs, when an output is spent it gets deleted from the UnspendOutput table and the action of the delete is represented in the inputs table,
   /// when a rewind happens we need to bring back outputs that have been deleted from the UnspendOutput so we look for those outputs in the inputs table,
   /// however the block index in the inputs table is the one representing the input not the output we are trying to restore so we have to look it up in the outputs table.
   /// </summary>
   private static Task MergeRewindInputsToUnspentTransactionsAsync(MongoDb storage, long blockIndex)
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
}
