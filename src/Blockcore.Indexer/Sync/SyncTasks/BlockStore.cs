using System.Collections.Generic;
using Blockcore.Indexer.Storage.Mongo.Types;

namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System;
   using System.Linq;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Client.Types;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

   /// <summary>
   /// The block sync.
   /// </summary>
   public class BlockStore : TaskRunner<StorageBatch>
   {
      private readonly ILogger<BlockStore> log;

      private readonly IStorageOperations storageOperations;
      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly System.Diagnostics.Stopwatch watch;
      private readonly Queue<(long count, long size, double seconds)> insertStats;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockStore"/> class.
      /// </summary>
      public BlockStore(
         IOptions<IndexerSettings> configuration,
         ILogger<BlockStore> logger,
         IStorageOperations storageOperations,
         ISyncOperations syncOperations,
         SyncConnection syncConnection)
          : base(configuration, logger)
      {
         this.storageOperations = storageOperations;
         this.syncOperations = syncOperations;
         this.syncConnection = syncConnection;
         log = logger;
         watch = Stopwatch.Start();
         insertStats = new Queue<(long count, long size, double seconds)>();
      }

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (Runner.SyncingBlocks.ReorgMode == true)
         {
            Runner.SyncingBlocks.StoreTip = await syncOperations.RewindToBestChain(syncConnection);
            Runner.SyncingBlocks.PullingTip = null;
            Queue.Clear();
            Runner.SyncingBlocks.ReorgMode = false;
            return false;
         }

         if (TryDequeue(out StorageBatch item))
         {
            // log.LogDebug($"Add to batch block = {item.BlockInfo.Height}({item.BlockInfo.Hash}) batch size = {(decimal)Runner.SyncingBlocks.StorageBatch.TotalSize / 1000000}mb");

            // check all blocks are consecutive and start from the last block in store.
            string prevHash = Runner.SyncingBlocks.StoreTip.BlockHash;
            foreach (MapBlock mapBlock in item.MapBlocks.Values.OrderBy(b => b.BlockIndex))
            {
               if (mapBlock.PreviousBlockHash != prevHash)
               {
                  throw new ApplicationException("None consecutive block received");
               }

               prevHash = mapBlock.BlockHash;
            }

            long count = item.MapBlocks.Count;
            long size = item.TotalSize;

            watch.Restart();

            Runner.SyncingBlocks.StoreTip = storageOperations.PushStorageBatch(item);

            watch.Stop();

            insertStats.Enqueue((count, size, watch.Elapsed.TotalSeconds));

            if (insertStats.Count > 100)
               insertStats.Dequeue();

            long totalBlocks = insertStats.Sum((tuple => tuple.count));
            double totalSeconds = insertStats.Sum((tuple => tuple.seconds));
            double avgBlocks = totalBlocks / totalSeconds;
            double avgSeconds = totalSeconds / totalBlocks;

            log.LogDebug($"Pushed {count} blocks tip = {Runner.SyncingBlocks.StoreTip.BlockIndex}({Runner.SyncingBlocks.StoreTip.BlockHash}) total Size = {((decimal)size / 1000000):0.00}mb Seconds = {watch.Elapsed.TotalSeconds} avg insert {avgBlocks:0.00}b/s ({avgSeconds:0.00}s/b)");

            //if (item.BlockInfo != null)
            //{
            //   if (!Runner.SyncingBlocks.CurrentSyncing.TryRemove(item.BlockInfo.Hash, out BlockInfo blockInfo))
            //   {
            //      throw new Exception(string.Format("Failed to remove block hash {0} from collection", item.BlockInfo.Hash));
            //   }

            //   syncConnection.RecentItems.Add((DateTime.UtcNow, watch.Elapsed, item.BlockInfo.Size));
            //}

            var notifications = new AddressNotifications { Addresses = new List<string>() };// count.Items.Where(ad => ad.Addresses != null).SelectMany(s => s.Addresses).Distinct().ToList() };
            Runner.Get<Notifier>().Enqueue(notifications);

            //  watch.Stop();

            //string message = item.BlockInfo != null ?
            //    string.Format("Seconds = {0} - BlockIndex = {1} - TotalItems = {2} - Size = {3} kb", watch.Elapsed.TotalSeconds, item.BlockInfo.Height, count.Transactions + count.InputsOutputs, item.BlockInfo.Size) :
            //    string.Format("Seconds = {0} - PoolSync - TotalItems = {1}", watch.Elapsed.TotalSeconds, count.Transactions + count.InputsOutputs);

            //log.LogDebug(message);

            return await Task.FromResult(true);
         }

         return await Task.FromResult(false);
      }
   }
}
