using System;
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
      public const int ExpectedNumberOfIndexes = 6;

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

            List<IndexView> indexes = mongoData.GetIndexesBuildProgress();
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

         List<IndexView> ops = mongoData.GetIndexesBuildProgress();

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
                        .IndexKeys.Hashed(trxBlk => trxBlk.TransactionId)));
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
                        .IndexKeys.Hashed(trxBlk => trxBlk.Outpoint)));

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
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(async task =>
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
                        .IndexKeys.Hashed(_=> _.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(UnspentOutputTable)}.{nameof(UnspentOutputTable.Address)}");

                  await mongoData.UnspentOutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating indexes on {nameof(UnspentOutputTable)}.{nameof(UnspentOutputTable.BlockIndex)}");

                  await mongoData.UnspentOutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

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
            if (indexingCompletTask is not { IsCompleted: true })
            {
               log.LogDebug($"Indexer - Indexing tables time passed {watch.Elapsed}");
            }
            else
            {
               List<string> allIndexes = mongoData.GetBlockIndexIndexes();

               if (allIndexes.Count != ExpectedNumberOfIndexes)
               {
                  throw new ApplicationException($"Expected {ExpectedNumberOfIndexes} indexes but got {allIndexes.Count}");
               }

               Runner.GlobalState.IndexMode = false;
               Runner.GlobalState.IndexModeCompleted = true;

               log.LogDebug($"Indexer - Indexing completed in {watch.Elapsed}");

               Abort = true;
               return true;
            }
         }

         return await Task.FromResult(false);
      }
   }
}
