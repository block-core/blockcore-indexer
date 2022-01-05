using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
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
                  log.LogDebug($"Creating indexes on {nameof(BlockTable)}.{nameof(BlockTable.BlockHash)}");

                  await mongoData.BlockTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<BlockTable>(Builders<BlockTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockHash)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(TransactionBlockTable)}.{nameof(TransactionBlockTable.BlockIndex)}");

                  await mongoData.TransactionBlockTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<TransactionBlockTable>(Builders<TransactionBlockTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(TransactionBlockTable)}.{nameof(TransactionBlockTable.TransactionId)}");

                  await mongoData.TransactionBlockTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<TransactionBlockTable>(Builders<TransactionBlockTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.TransactionId)));
               })
               .ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(OutputTable)}.{nameof(OutputTable.BlockIndex)}");

                  await mongoData.OutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<OutputTable>(Builders<OutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(OutputTable)}.{nameof(OutputTable.Outpoint)}");

                  await mongoData.OutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<OutputTable>(Builders<OutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(OutputTable)}.{nameof(OutputTable.Address)}");

                  await mongoData.OutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<OutputTable>(Builders<OutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));
               })
               .ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(InputTable)}.{nameof(InputTable.BlockIndex)}");

                  await mongoData.InputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<InputTable>(Builders<InputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(InputTable)}.{nameof(InputTable.Outpoint)}");

                  await mongoData.InputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<InputTable>(Builders<InputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(InputTable)}.{nameof(InputTable.Address)}");

                  await mongoData.InputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<InputTable>(Builders<InputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(async task =>
               {
                  // run this indexes together because they data store should be empty they will complete fast

                  log.LogDebug($"Creating indexes on {nameof(AddressComputedTable)}.{nameof(AddressComputedTable.Address)}");

                  await mongoData.AddressComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressComputedTable>(Builders<AddressComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

                  // --- history
                  log.LogDebug($"Creating indexes on {nameof(AddressHistoryComputedTable)}.{nameof(AddressHistoryComputedTable.BlockIndex)}");

                  await mongoData.AddressHistoryComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputedTable>(Builders<AddressHistoryComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

                  log.LogDebug($"Creating indexes on {nameof(AddressHistoryComputedTable)}.{nameof(AddressHistoryComputedTable.Position)}");

                  await mongoData.AddressHistoryComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputedTable>(Builders<AddressHistoryComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Position)));

                  log.LogDebug($"Creating indexes on {nameof(AddressHistoryComputedTable)}.{nameof(AddressHistoryComputedTable.Address)}");

                  await mongoData.AddressHistoryComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputedTable>(Builders<AddressHistoryComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

                  // -- utxo
                  log.LogDebug($"Creating indexes on {nameof(AddressUtxoComputedTable)}.{nameof(AddressUtxoComputedTable.BlockIndex)}");

                  await mongoData.AddressUtxoComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressUtxoComputedTable>(Builders<AddressUtxoComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

                  log.LogDebug($"Creating indexes on {nameof(AddressUtxoComputedTable)}.{nameof(AddressUtxoComputedTable.Outpoint)}");

                  await mongoData.AddressUtxoComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressUtxoComputedTable>(Builders<AddressUtxoComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint), new CreateIndexOptions {Unique = true}));

                  log.LogDebug($"Creating indexes on {nameof(AddressUtxoComputedTable)}.{nameof(AddressUtxoComputedTable.Address)}");

                  await mongoData.AddressUtxoComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressUtxoComputedTable>(Builders<AddressUtxoComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

                  // --mempool
                  log.LogDebug($"Creating indexes on {nameof(MempoolTable)}.{nameof(MempoolTable.TransactionId)}");

                  await mongoData.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<MempoolTable>(Builders<MempoolTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.TransactionId)));

                  log.LogDebug($"Creating indexes on {nameof(MempoolTable)}.{nameof(MempoolTable.AddressOutputs)}");

                  await mongoData.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<MempoolTable>(Builders<MempoolTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.AddressOutputs)));

                  log.LogDebug($"Creating indexes on {nameof(MempoolTable)}.{nameof(MempoolTable.AddressInputs)}");

                  await mongoData.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<MempoolTable>(Builders<MempoolTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.AddressInputs)));
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
                  var addressNulls = mongoData.InputTable.AsQueryable()
                     .OrderBy(b => b.BlockIndex)
                     .Where(w => w.Address == null).Take(1).ToList();

                  if (addressNulls.Any())
                  {
                     inputCopyLastBlockHeight = addressNulls.First().BlockIndex;
                  }
               }

               if (inputCopyLastBlockHeight != null)
               {
                  long blocksToCopy = 5;
                  watch.Restart();

                  long startHeigt = inputCopyLastBlockHeight.Value;
                  var tasks = new List<Task>();
                  var exec = new List<(long last, long blc)>();
                  for (int i = 0; i < 5; i++)
                  {
                     exec.Add((inputCopyLastBlockHeight.Value, blocksToCopy));
                     inputCopyLastBlockHeight += blocksToCopy;
                  }

                  foreach ((long last, long blc) valueTuple in exec)
                  {
                     tasks.Add(Task.Run(async () =>
                     {
                        PipelineDefinition<InputTable, InputTable> pipeline = BuildInputsAddressUpdatePiepline((int)valueTuple.last, (int)valueTuple.blc);

                        await mongoData.InputTable.AggregateAsync(pipeline);

                     }));
                  }

                  Task.WaitAll(tasks.ToArray());

                  double totalSeconds = watch.Elapsed.TotalSeconds;
                  long totalBlocks = exec.Sum(s => s.blc);
                  double blocksPerSecond = totalBlocks / totalSeconds;

                  log.LogDebug($"Indexer - Copied input addresses for {totalBlocks} blocks, from height {startHeigt} to height {startHeigt + totalBlocks}, Seconds = {totalSeconds} - {blocksPerSecond:0.00}b/s");

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

                  log.LogDebug($"Indexer - Indexing completed");

                  Abort = true;
                  return true;
               }
            }
            else
            {
               log.LogDebug($"Indexer - Indexing tables time passed {watch.Elapsed}");
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
      public static PipelineDefinition<InputTable, InputTable> BuildInputsAddressUpdatePiepline(int fromBlockIndex = 0, int? blocksToTake = int.MaxValue)
      {
         PipelineDefinition<InputTable, InputTable> pipline = new[]
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
                  { "from", "Output" },
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
                  { "into", "Input" },
                  { "on", "_id" },
                  { "whenMatched", "merge" },
                  { "whenNotMatched", "insert" }
               })
         };

         return pipline;
      }
   }
}
