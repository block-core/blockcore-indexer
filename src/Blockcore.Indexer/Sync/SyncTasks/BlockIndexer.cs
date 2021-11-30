using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

      long? inputCopyLastBlockHeight;

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
                  //log.LogDebug($"Updating data on {nameof(AddressForInput)}.{nameof(AddressForInput.Address)} and {nameof(AddressForInput)}.{nameof(AddressForInput.Value)}");

                  //PipelineDefinition<AddressForInput, AddressForInput> pipeline = BuildInputsAddressUpdatePiepline();

                  //await mongoData.AddressForInput.AggregateAsync(pipeline);
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
               if (inputCopyLastBlockHeight == null)
               {
                  IQueryable<AddressForInput> addressNulls = mongoData.AddressForInput.AsQueryable()
                     .OrderBy(b => b.BlockIndex)
                     .Where(w => w.Address == null).Take(1);
                  
                  if (addressNulls.Any())
                  {
                     inputCopyLastBlockHeight = addressNulls.First().BlockIndex;
                  }
               }

               if (inputCopyLastBlockHeight != null)
               {
                  long blocksToCopy = 10;
                  watch.Restart();

                  long startHeigt = inputCopyLastBlockHeight.Value;
                  var tasks = new List<Task>();
                  var exec = new List<(long last, long blc)>();
                  for (int i = 0; i < 10; i++)
                  {
                     exec.Add((inputCopyLastBlockHeight.Value, blocksToCopy));
                     inputCopyLastBlockHeight += blocksToCopy;
                  }

                  foreach ((long last, long blc) valueTuple in exec)
                  {
                     tasks.Add(Task.Run(async () =>
                     {
                        PipelineDefinition<AddressForInput, AddressForInput> pipeline = BuildInputsAddressUpdatePiepline((int)valueTuple.last, (int)valueTuple.blc);

                        await mongoData.AddressForInput.AggregateAsync(pipeline);

                     }));
                  }

                  Task.WaitAll(tasks.ToArray());

                  double totalSeconds = watch.Elapsed.TotalSeconds;
                  long totalBlocks = exec.Sum(s => s.blc);
                  double blocksPerSecond = totalBlocks / totalSeconds;

                  log.LogDebug($"Copied input addresses for {totalBlocks} blocks, from height {startHeigt} to height {startHeigt + totalBlocks}, Seconds = {totalSeconds} - {blocksPerSecond:0.00}b/s");



                  //double blocksPerSecond = blocksToCopy / totalSeconds;
                  //double secondsPerBlock = totalSeconds / blocksToCopy;

                  //log.LogDebug($"Copied input addresses, from height {inputCopyLastBlockHeight} to height {inputCopyLastBlockHeight + blocksToCopy}, Seconds = {totalSeconds} - {blocksPerSecond:0.00}b/s");

                  //inputCopyLastBlockHeight += blocksToCopy;

                  if (inputCopyLastBlockHeight >= Runner.GlobalState.StoreTip.BlockIndex)
                  {
                     inputCopyLastBlockHeight = null;
                  }

                  return true;
               }
               else
               {
                  Runner.GlobalState.IndexMode = false;
                  Runner.GlobalState.IndexModeCompleted = true;

                  log.LogDebug($"Indexing completed");

                  Abort = true;
                  return true;
               }
            }
            else
            {
               log.LogDebug($"Indexing tables time passed {watch.Elapsed}");
            }
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
      public static PipelineDefinition<AddressForInput, AddressForInput> BuildInputsAddressUpdatePiepline(int fromBlockIndex = 0, int? blocksToTake = int.MaxValue)
      {
         PipelineDefinition<AddressForInput, AddressForInput> pipline = new[]
         {
            new BsonDocument("$match",
               new BsonDocument
               {
                  { "BlockIndex",
                     new BsonDocument
                     {
                        { "$gte", fromBlockIndex },
                        { "$lt", fromBlockIndex + blocksToTake }
                     } },
                  { "Address", BsonNull.Value }
               }),
            new BsonDocument("$project",
               new BsonDocument
               {
                  { "_id", "$_id" },
                  { "Outpoint", "$Outpoint" },
               }),
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "AddressForOutput" },
                  { "localField", "Outpoint" },
                  { "foreignField", "Outpoint" },
                  { "pipeline", new BsonArray
                     {
                        new BsonDocument("$project",
                           new BsonDocument
                           {
                              { "Address", "$Address" },
                              { "Value", "$Value" }
                           })
                     } },
                  { "as", "output" }
               }),
            new BsonDocument("$unwind",
               new BsonDocument("path", "$output")),
            new BsonDocument("$project",
               new BsonDocument
               {
                  { "_id", "$_id" },
                  { "Address", "$output.Address" },
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
