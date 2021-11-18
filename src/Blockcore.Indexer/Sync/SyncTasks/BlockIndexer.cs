using System;
using System.Collections.Generic;
using System.Threading;
using Blockcore.Indexer.Client;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Storage.Mongo.Types;
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

   /// <summary>
   /// The block sync.
   /// </summary>
   public class BlockIndexer : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<BlockIndexer> log;

      private readonly IStorageOperations storageOperations;
      readonly IStorage data;

      private readonly System.Diagnostics.Stopwatch watch;

      private MongoData mongoData;

      private bool completed;

      Task indexingTask;
      Task indexingCompletTask;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockPuller"/> class.
      /// </summary>
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

         if (Runner.SyncingBlocks.Blocked)
         {
            return false;
         }

         if (Runner.SyncingBlocks.ReorgMode)
         {
            return false;
         }

         if (Runner.SyncingBlocks.StoreTip == null)
         {
            return false;
         }

         if (Runner.SyncingBlocks.IndexMode == false)
         {
            if (Runner.SyncingBlocks.IbdMode() == true)
            {
               return false;
            }

            Runner.SyncingBlocks.IndexMode = true;
         }

         if (indexingTask == null)
         {
            // build indexing tasks

            indexingTask = Task.Run(async () =>
               {
                  log.LogDebug($"Creating {nameof(MapBlock)}.{nameof(MapBlock.BlockHash)} indexes");

                  await mongoData.MapBlock.Indexes
                     .CreateOneAsync(new CreateIndexModel<MapBlock>(Builders<MapBlock>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockHash)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(MapTransactionBlock)}.{nameof(MapTransactionBlock.BlockIndex)} indexes");

                  await mongoData.MapTransactionBlock.Indexes
                     .CreateOneAsync(new CreateIndexModel<MapTransactionBlock>(Builders<MapTransactionBlock>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(MapTransactionBlock)}.{nameof(MapTransactionBlock.TransactionId)} indexes");

                  await mongoData.MapTransactionBlock.Indexes
                     .CreateOneAsync(new CreateIndexModel<MapTransactionBlock>(Builders<MapTransactionBlock>
                        .IndexKeys.Ascending(trxBlk => trxBlk.TransactionId)));
               })
               .ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(AddressForOutput)}.{nameof(AddressForOutput.BlockIndex)} indexes");

                  await mongoData.AddressForOutput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForOutput>(Builders<AddressForOutput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(AddressForOutput)}.{nameof(AddressForOutput.Outpoint)} indexes");

                  await mongoData.AddressForOutput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForOutput>(Builders<AddressForOutput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(AddressForOutput)}.{nameof(AddressForOutput.Address)} indexes");

                  await mongoData.AddressForOutput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForOutput>(Builders<AddressForOutput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));
               })
               .ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(AddressForInput)}.{nameof(AddressForInput.BlockIndex)} indexes");

                  await mongoData.AddressForInput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForInput>(Builders<AddressForInput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(AddressForInput)}.{nameof(AddressForInput.Outpoint)} indexes");

                  await mongoData.AddressForInput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForInput>(Builders<AddressForInput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint)));

               }).ContinueWith(async task =>
               {
                  log.LogDebug($"Creating {nameof(AddressForInput)}.{nameof(AddressForInput.Address)} indexes");

                  await mongoData.AddressForInput.Indexes
                     .CreateOneAsync(new CreateIndexModel<AddressForInput>(Builders<AddressForInput>
                        .IndexKeys.Ascending(trxBlk => trxBlk.Address)));

               }).ContinueWith(task =>
               {
                  indexingCompletTask = task;
               });
         }
         else
         {
            if (indexingCompletTask != null && indexingCompletTask.IsCompleted)
            {
               Runner.SyncingBlocks.IndexMode = false;

               Abort = true;
               return true;
            }

            log.LogDebug($"Indexing tables...");
         }

         return await Task.FromResult(false);
      }
   }
}
