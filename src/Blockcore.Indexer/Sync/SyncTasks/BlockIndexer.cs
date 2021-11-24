using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Blockcore.Indexer.Client;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System.Linq;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

   public class BlockIndexer : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<BlockIndexer> log;

      private readonly IStorageOperations storageOperations;
      readonly IStorage data;

      private readonly System.Diagnostics.Stopwatch watch;

      private readonly MongoData mongoData;

      Task indexingTask;
      Task indexingCompletTask;
      bool initialized;

      public BlockIndexer(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         ILogger<BlockIndexer> logger,
         IStorageOperations storageOperations,
         IStorage data)
          : base(configuration, logger)
      {
         log = logger;
         this.storageOperations = storageOperations;
         this.data = data;
         this.syncConnection = syncConnection;
         this.syncOperations = syncOperations;
         config = configuration.Value;
         watch = Stopwatch.Start();

         mongoData = (MongoData)data;
         
      }

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (!config.SyncBlockchain)
         {
            Abort = true;
            return true;
         }

         if (Runner.GlobalState.Blocked)
         {
            return false;
         }

         if (Runner.GlobalState.ReorgMode)
         {
            return false;
         }

         if (Runner.GlobalState.StoreTip == null)
         {
            return false;
         }

         if (initialized == false)
         {
            initialized = true;

            List<IndexView> indexes = mongoData.GetCurrentIndexes();
            if (indexes.Any())
            {
               // if indexes are currently running go directly in to index mode
               Runner.GlobalState.IndexMode = true;
               return false;
            }
         }

         if (Runner.GlobalState.IndexMode == false)
         {
            if (Runner.GlobalState.IbdMode() == true)
            {
               return false;
            }

            Runner.GlobalState.IndexMode = true;
         }

         List<IndexView> ops = mongoData.GetCurrentIndexes();

         if (ops.Any())
         {
            var stringBuilder = new StringBuilder();
            foreach (IndexView op in ops)
            {
               stringBuilder.AppendLine(op.Command + op.Msg);
               
            }

            log.LogDebug(stringBuilder.ToString());

            return false;
         }

         if (indexingTask == null)
         {
            // build indexing tasks
            watch.Restart();

            indexingTask = Task.Run(async () =>
               {
                  log.LogDebug($"Creating indexes on {nameof(MapBlock)}.{nameof(MapBlock.BlockHash)}");

                  await mongoData.MapBlock.Indexes
                     .CreateOneAsync(new CreateIndexModel<MapBlock>(Builders<MapBlock>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockHash)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(MapTransactionBlock)}.{nameof(MapTransactionBlock.BlockIndex)}");

                  await mongoData.MapTransactionBlock.Indexes
                     .CreateOneAsync(new CreateIndexModel<MapTransactionBlock>(Builders<MapTransactionBlock>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(MapTransactionBlock)}.{nameof(MapTransactionBlock.TransactionId)}");

                  await mongoData.MapTransactionBlock.Indexes
                     .CreateOneAsync(new CreateIndexModel<MapTransactionBlock>(Builders<MapTransactionBlock>
                        .IndexKeys.Ascending(trxBlk => trxBlk.TransactionId)));
               })
               .ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(AddressForOutput)}.{nameof(AddressForOutput.BlockIndex)}");

                  await mongoData.AddressForOutput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForOutput>(Builders<AddressForOutput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(AddressForOutput)}.{nameof(AddressForOutput.Outpoint)}");

                  await mongoData.AddressForOutput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForOutput>(Builders<AddressForOutput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(AddressForOutput)}.{nameof(AddressForOutput.Address)}");

                  await mongoData.AddressForOutput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForOutput>(Builders<AddressForOutput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));
               })
               .ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(AddressForInput)}.{nameof(AddressForInput.BlockIndex)}");

                  await mongoData.AddressForInput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForInput>(Builders<AddressForInput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(AddressForInput)}.{nameof(AddressForInput.Outpoint)}");

                  await mongoData.AddressForInput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForInput>(Builders<AddressForInput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(AddressForInput)}.{nameof(AddressForInput.Address)}");

                  await mongoData.AddressForInput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForInput>(Builders<AddressForInput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(async task =>
               {
                  // run this indexes together because they data store should be empty they will complete fast

                  log.LogDebug($"Creating indexes on {nameof(AddressComputed)}.{nameof(AddressComputed.Address)}");

                  await mongoData.AddressComputed.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressComputed>(Builders<AddressComputed>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));


                  log.LogDebug($"Creating indexes on {nameof(AddressHistoryComputed)}.{nameof(AddressHistoryComputed.BlockIndex)}");

                  await mongoData.AddressHistoryComputed.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputed>(Builders<AddressHistoryComputed>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

                  log.LogDebug($"Creating indexes on {nameof(AddressHistoryComputed)}.{nameof(AddressHistoryComputed.Position)}");

                  await mongoData.AddressHistoryComputed.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputed>(Builders<AddressHistoryComputed>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Position)));

                  log.LogDebug($"Creating indexes on {nameof(Mempool)}.{nameof(Mempool.TransactionId)}");

                  await mongoData.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<Mempool>(Builders<Mempool>
                        .IndexKeys.Ascending(trxBlk => trxBlk.TransactionId)));

                  log.LogDebug($"Creating indexes on {nameof(Mempool)}.{nameof(Mempool.AddressOutputs)}");

                  await mongoData.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<Mempool>(Builders<Mempool>
                        .IndexKeys.Ascending(trxBlk => trxBlk.AddressOutputs)));

                  log.LogDebug($"Creating indexes on {nameof(Mempool)}.{nameof(Mempool.AddressInputs)}");

                  await mongoData.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<Mempool>(Builders<Mempool>
                        .IndexKeys.Ascending(trxBlk => trxBlk.AddressInputs)));
               })

               .ContinueWith(async task =>
               {
                  log.LogDebug($"Updating data on {nameof(AddressForInput)}.{nameof(AddressForInput.Address)} and {nameof(AddressForInput)}.{nameof(AddressForInput.Value)}");

                  PipelineDefinition<AddressForInput, AddressForInput> pipeline = BuildInputsAddressUpdatePiepline();

                  await mongoData.AddressForInput.AggregateAsync(pipeline);
               })
               .ContinueWith(task =>
               {
                  indexingCompletTask = task;
               });
         }
         else
         {
            if (indexingCompletTask != null && indexingCompletTask.IsCompleted)
            {
               Runner.GlobalState.IndexMode = false;
               Runner.GlobalState.IndexModeCompleted = true;

               log.LogDebug($"Indexing completed");

               Abort = true;
               return true;
            }

            log.LogDebug($"Indexing tables time passed {watch.Elapsed}");
         }

         return await Task.FromResult(false);
      }

      /// <summary>
      /// Update all addresses on the inputs table with the address and value form the outputs table 
      /// Build a mongodb pipeline that will:
      /// - iterate over all inputs
      /// - filter all addresses that are not null
      /// - match with outputs table to find the address and value an input is spending from
      /// - update the input with the address and value
      /// </summary>
      public static PipelineDefinition<AddressForInput, AddressForInput> BuildInputsAddressUpdatePiepline()
      {
         PipelineDefinition<AddressForInput,AddressForInput> pipline = new []
         {
            new BsonDocument("$match",
               new BsonDocument("Address", BsonNull.Value)),
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "AddressForOutput" },
                  { "localField", "Outpoint" },
                  { "foreignField", "Outpoint" },
                  { "as", "output" }
               }),
            new BsonDocument("$unwind",
               new BsonDocument("path", "$output")),
            new BsonDocument("$project",
               new BsonDocument
               {
                  { "_id", "$_id" },
                  { "Outpoint", "$Outpoint" },
                  { "Address", "$output.Address" },
                  { "BlockIndex", "$BlockIndex" },
                  { "TrxHash", "$TrxHash" },
                  { "Value", "$output.Value" }
               }),
            new BsonDocument("$merge",
               new BsonDocument
               {
                  { "into", "AddressForInput" },
                  { "on", "_id" },
                  { "whenMatched", "merge" },
                  { "whenNotMatched", "insert" }
               })
         };

         return pipline;
      }
   }
}
