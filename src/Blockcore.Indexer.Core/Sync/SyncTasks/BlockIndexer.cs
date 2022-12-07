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
      private readonly IndexerSettings config;
      private readonly ILogger<BlockIndexer> log;
      readonly IStorage data;

      private readonly System.Diagnostics.Stopwatch watch;

      private readonly IMongoDb db;

      Task indexingTask;
      Task indexingCompletTask;
      bool initialized;

      public BlockIndexer(
         IOptions<IndexerSettings> configuration,
         ILogger<BlockIndexer> logger,
         IStorage data, IMongoDb db)
          : base(configuration, logger)
      {
         log = logger;
         this.data = data;
         this.db = db;
         config = configuration.Value;
         watch = Stopwatch.Start();

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

            List<IndexView> indexes = data.GetIndexesBuildProgress();
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

         List<IndexView> ops = data.GetIndexesBuildProgress();

         if (ops.Any())
         {
            var stringBuilder = new StringBuilder();
            foreach (IndexView op in ops)
            {
               stringBuilder.AppendLine(op.Command + op.Msg);

            }

            log.LogInformation(stringBuilder.ToString());

            return false;
         }

         if (indexingTask == null)
         {
            // build indexing tasks
            watch.Restart();

            indexingTask = Task.Run(async () =>
               {
                  log.LogInformation($"Creating indexes on {nameof(BlockTable)}.{nameof(BlockTable.BlockHash)}");

                  await db.BlockTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<BlockTable>(Builders<BlockTable>
                        .IndexKeys.Descending(trxBlk => trxBlk.BlockHash)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(TransactionBlockTable)}.{nameof(TransactionBlockTable.BlockIndex)}");

                  await db.TransactionBlockTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<TransactionBlockTable>(Builders<TransactionBlockTable>
                        .IndexKeys.Descending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(TransactionBlockTable)}.{nameof(TransactionBlockTable.TransactionId)}");

                  await db.TransactionBlockTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<TransactionBlockTable>(Builders<TransactionBlockTable>
                        .IndexKeys.Hashed(trxBlk => trxBlk.TransactionId)));
               })
               .ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(OutputTable)}.{nameof(OutputTable.BlockIndex)}");

                  await db.OutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<OutputTable>(Builders<OutputTable>
                        .IndexKeys.Descending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(OutputTable)}.{nameof(OutputTable.Outpoint)}");

                  await db.OutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<OutputTable>(Builders<OutputTable>
                        .IndexKeys.Hashed(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(OutputTable)}.{nameof(OutputTable.Address)}");

                  await db.OutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<OutputTable>(Builders<OutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));
               })
               .ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(InputTable)}.{nameof(InputTable.BlockIndex)}");

                  await db.InputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<InputTable>(Builders<InputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(InputTable)}.{nameof(InputTable.BlockIndex)}");

                  await db.InputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<InputTable>(Builders<InputTable>
                        .IndexKeys.Descending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(InputTable)}.{nameof(InputTable.Outpoint)}");

                  await db.InputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<InputTable>(Builders<InputTable>
                        .IndexKeys.Hashed(_=> _.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(UnspentOutputTable)}.{nameof(UnspentOutputTable.Address)}");

                  await db.UnspentOutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(async task =>
               {
                  log.LogInformation($"Creating indexes on {nameof(UnspentOutputTable)}.{nameof(UnspentOutputTable.BlockIndex)}");

                  await db.UnspentOutputTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  // run this indexes together because they data store should be empty they will complete fast

                  log.LogInformation($"Creating indexes on {nameof(AddressComputedTable)}.{nameof(AddressComputedTable.Address)}");

                  await db.AddressComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressComputedTable>(Builders<AddressComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

                  // --- history
                  log.LogInformation($"Creating indexes on {nameof(AddressHistoryComputedTable)}.{nameof(AddressHistoryComputedTable.BlockIndex)}");

                  await db.AddressHistoryComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputedTable>(Builders<AddressHistoryComputedTable>
                        .IndexKeys.Descending(trxBlk => trxBlk.BlockIndex)));

                  log.LogInformation($"Creating indexes on {nameof(AddressHistoryComputedTable)}.{nameof(AddressHistoryComputedTable.Position)}");

                  await db.AddressHistoryComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputedTable>(Builders<AddressHistoryComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Position)));

                  log.LogInformation($"Creating indexes on {nameof(AddressHistoryComputedTable)}.{nameof(AddressHistoryComputedTable.Address)}");

                  await db.AddressHistoryComputedTable.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressHistoryComputedTable>(Builders<AddressHistoryComputedTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

                  // -- utxo
                  log.LogInformation($"Creating indexes on {nameof(AddressUtxoComputedTable)}.{nameof(AddressUtxoComputedTable.BlockIndex)}");

                  // --mempool
                  log.LogInformation($"Creating indexes on {nameof(MempoolTable)}.{nameof(MempoolTable.TransactionId)}");

                  await db.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<MempoolTable>(Builders<MempoolTable>
                        .IndexKeys.Hashed(trxBlk => trxBlk.TransactionId)));

                  log.LogInformation($"Creating indexes on {nameof(MempoolTable)}.{nameof(MempoolTable.AddressOutputs)}");

                  await db.Mempool.Indexes
                     .CreateOneAsync(new CreateIndexModel<MempoolTable>(Builders<MempoolTable>
                        .IndexKeys.Ascending(trxBlk => trxBlk.AddressOutputs)));

                  log.LogInformation($"Creating indexes on {nameof(MempoolTable)}.{nameof(MempoolTable.AddressInputs)}");

                  await db.Mempool.Indexes
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
               log.LogInformation($"Indexer - Indexing tables time passed {watch.Elapsed}");
            }
            else
            {
               List<string> allIndexes = data.GetBlockIndexIndexes();

               if (allIndexes.Count != config.IndexCountForBlockIndexProperty)
               {
                  throw new ApplicationException($"Expected {config.IndexCountForBlockIndexProperty} indexes but got {allIndexes.Count}");
               }

               Runner.GlobalState.IndexMode = false;
               Runner.GlobalState.IndexModeCompleted = true;

               log.LogInformation($"Indexer - Indexing completed in {watch.Elapsed}");

               Abort = true;
               return true;
            }
         }

         return await Task.FromResult(false);
      }
   }
}
