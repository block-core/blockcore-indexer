using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   public class FeeEstimatorSync : TaskRunner
   {
      private readonly IMongoDb db;
      private readonly ILogger<FeeEstimatorSync> log;
      private readonly IndexerSettings indexerSettings;

      private readonly Stopwatch watch;

      readonly ICryptoClientFactory clientFactory;
      private readonly SyncConnection syncConnection;

      private bool syncInProgress;
      DateTime lastSync;

      public FeeEstimatorSync(
         IOptions<IndexerSettings> configuration,
         ILogger<FeeEstimatorSync> logger,
         IMongoDb data,
         SyncConnection syncConnection,
         ICryptoClientFactory clientFactory)
         : base(configuration, logger)
      {
         db = data;
         log = logger;
         watch = new Stopwatch();
         indexerSettings = configuration.Value;
         this.clientFactory = clientFactory;
         this.syncConnection = syncConnection;

      }

      public override async Task<bool> OnExecute()
      {
         if (!CanRunFeeEstimatorSync())
         {
            return false;
         }

         try
         {
            syncInProgress = true;

            log.LogDebug($"Starting fee estimation");
            watch.Restart();

            var client = clientFactory.Create(syncConnection);

            var wait_1_blocks = await client.EstimateSmartFeeAsync(1);
            var wait_5_blocks = await client.EstimateSmartFeeAsync(5);
            var wait_10_blocks = await client.EstimateSmartFeeAsync(10);
            var wait_50_blocks = await client.EstimateSmartFeeAsync(50);
            var wait_100_blocks = await client.EstimateSmartFeeAsync(100);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"fee for {nameof(wait_1_blocks)} = {wait_1_blocks.FeeRate}");
            sb.AppendLine($"fee for {nameof(wait_5_blocks)} = {wait_5_blocks.FeeRate}");
            sb.AppendLine($"fee for {nameof(wait_10_blocks)} = {wait_10_blocks.FeeRate}");
            sb.AppendLine($"fee for {nameof(wait_50_blocks)} = {wait_50_blocks.FeeRate}");
            sb.AppendLine($"fee for {nameof(wait_100_blocks)} = {wait_100_blocks.FeeRate}");

            // todo insert to db

            log.LogInformation(sb.ToString());

            watch.Stop();

            log.LogDebug($"Finished fee estimation in {watch.Elapsed}");
         }
         catch (Exception e)
         {
            log.LogError(e, "Failed to fetch fee estimation");
            lastSync = new DateTime();
         }
         finally
         {
            syncInProgress = false;
         }

         lastSync = DateTime.UtcNow;
         return await Task.FromResult(false);
      }

      private bool CanRunFeeEstimatorSync()
      {
         if (indexerSettings.SyncFeeEstimator == false)
         {
            Abort = true;
            return false;
         }

         return !( //sync with other runners
            !Runner.GlobalState.IndexModeCompleted ||
            Runner.GlobalState.Blocked ||
            Runner.GlobalState.ReorgMode ||
            Runner.GlobalState.StoreTip == null ||
            Runner.GlobalState.IndexMode ||
            //local state valid
            syncInProgress ||
            lastSync.AddMinutes(10) > DateTime.UtcNow);
      }

   }
}
