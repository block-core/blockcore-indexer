using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public class BlockRewindOperation : IBlockRewindOperation
{
   protected IMongoDb storage;

   public BlockRewindOperation(IMongoDb storage)
   {
      this.storage = storage;
   }

   public async Task RewindBlockAsync(uint blockIndex)
   {
      await StoreRewindBlockAsync(storage, blockIndex);

      // this is an edge case, we delete from the utxo table in case a batch push failed half way and left
      // item in the utxo table that where suppose to get deleted, to avoid duplicates in recovery processes
      // we delete just in case (the utxo table has a unique key on outputs), there is no harm in deleting twice.
      Task<DeleteResult> unspentOutputBeforeInputTableRewind = DeleteFromCollectionByExpression(storage.UnspentOutputTable, _ => _.BlockIndex, blockIndex);

      Task<DeleteResult> output = DeleteFromCollectionByExpression(storage.OutputTable, _ => _.BlockIndex, blockIndex);

      // delete the transaction
      Task<DeleteResult> transactions = DeleteFromCollectionByExpression(storage.TransactionBlockTable, _ => _.BlockIndex, blockIndex);

      // delete computed
      Task<DeleteResult> addressComputed = DeleteFromCollectionByExpression(storage.AddressComputedTable, _ => _.ComputedBlockIndex, blockIndex);

      // delete computed history
      Task<DeleteResult> addressHistoryComputed = DeleteFromCollectionByExpression(storage.AddressHistoryComputedTable, _ => _.BlockIndex, blockIndex);

      await Task.WhenAll( output, transactions, addressComputed, addressHistoryComputed, unspentOutputBeforeInputTableRewind);

      if (!ValidateDeleteIsAcknowledged(output, transactions, addressComputed, addressHistoryComputed, unspentOutputBeforeInputTableRewind))
      {
         throw new InvalidOperationException("Not all delete operations completed successfully"); //Throw to start over and delete the block again
      }

      ConfirmDataDeletion(blockIndex);

      await MergeRewindInputsToUnspentTransactionsAsync(storage, blockIndex);

      Task<DeleteResult> inputs = DeleteFromCollectionByExpression(storage.InputTable, _ => _.BlockIndex, blockIndex);

      // TODO: if we filtered out outputs that where created and spent as part of the same block
      // we may not need to delete again, however there is no harm in this extra delete.
      var unspentOutput = DeleteFromCollectionByExpression(storage.UnspentOutputTable, _ => _.BlockIndex, blockIndex);

      await Task.WhenAll( inputs, unspentOutput);

      if (!ValidateDeleteIsAcknowledged(inputs, unspentOutput))
         throw new InvalidOperationException("Not all delete operations completed successfully"); //Throw to start over and delete the block again
   }

   private void ConfirmDataDeletion(uint blockIndex)
   {
      // todo: make sure all data was deleted for a given block
      // we saw instances where data was not completely deleted from the Output table, possibly due to mongo sync times.
   }

   private Task<DeleteResult> DeleteFromCollectionByExpression<TCollection,TField>(IMongoCollection<TCollection> collection,
      Expression<Func<TCollection,TField>> expression, TField value)
   {
      FilterDefinition<TCollection> filter = Builders<TCollection>.Filter.Eq(expression,value);

      return collection.DeleteManyAsync(filter);//TODO handle failed delete result
   }

   private bool ValidateDeleteIsAcknowledged(params Task<DeleteResult>[] tasks)
   {
      return tasks.All(_ => _.Result.IsAcknowledged);
   }

   private static Task StoreRewindBlockAsync(IMongoDb storage, uint blockIndex)
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


   /// <summary>
   /// Inputs spend outputs, when an output is spent it gets deleted from the UnspendOutput table and the action of the delete is represented in the inputs table,
   /// when a rewind happens we need to bring back outputs that have been deleted from the UnspendOutput so we look for those outputs in the inputs table,
   /// however the block index in the inputs table is the one representing the input not the output we are trying to restore so we have to look it up in the outputs table.
   /// </summary>
   private static async Task MergeRewindInputsToUnspentTransactionsAsync(IMongoDb storage, long blockIndex)
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

      if (unspentOutputs.Any())
      {
         // this is to unsure the values are unique
         unspentOutputs.ToDictionary(a => a.Outpoint.ToString());

         var duplicates = await storage.UnspentOutputTable.Find(
               Builders<UnspentOutputTable>.Filter.In(_ => _.Outpoint, unspentOutputs.Select(_ => _.Outpoint)))
            .ToListAsync();

         var filteredUnspentOutputs = duplicates.Any()
            ? unspentOutputs.Where(_ => !duplicates
                  .Exists(d => d.Outpoint == new Outpoint
                  {TransactionId = _.Outpoint.TransactionId, OutputIndex = _.Outpoint.OutputIndex}))
               .ToList()
            : unspentOutputs;

         // TODO: filter out any outputs that belong to the block being reorged.
         // this can happen for outputs that are created and spent in the same block.
         // if they get pushed now such outputs willjust get deleted in the next step.

         if (filteredUnspentOutputs.Any())
            await storage.UnspentOutputTable.InsertManyAsync(filteredUnspentOutputs);
      }
   }
}
