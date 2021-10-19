using System.Collections.Generic;

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
   public class BlockStore : TaskRunner<SyncBlockTransactionsOperation>
   {
      private readonly ILogger<BlockStore> log;

      private readonly IStorageOperations storageOperations;
      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly System.Diagnostics.Stopwatch watch;

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
      }

      private Queue<(int, long, double)> inserts = new Queue<(int, long, double)>();

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (Runner.SyncingBlocks.ReorgMode == true)
         {
            Runner.SyncingBlocks.StoreTip = await syncOperations.RewindToBestChain(syncConnection);
            Runner.SyncingBlocks.PullingTip = null;
            Runner.SyncingBlocks.StorageBatch.Clear();
            Runner.SyncingBlocks.ReorgMode = false;
            return false;
         }

         if (TryDequeue(out SyncBlockTransactionsOperation item))
         {
            // watch.Restart();

            // storageOperations.ValidateBlock(item);

            // InsertStats count = storageOperations.InsertTransactions(item);

            storageOperations.AddToStorageBatch(Runner.SyncingBlocks.StorageBatch, item);

            log.LogDebug($"Add to batch block = {item.BlockInfo.Height}({item.BlockInfo.Hash}) batch size = {(decimal)Runner.SyncingBlocks.StorageBatch.TotalSize / 1000000}mb");

            if (Runner.SyncingBlocks.StorageBatch.TotalSize > 20000000) // 10000000)
            {
               int count = Runner.SyncingBlocks.StorageBatch.MapBlocks.Count;
               long size = Runner.SyncingBlocks.StorageBatch.TotalSize;

               watch.Restart();

               storageOperations.PushStorageBatch(Runner.SyncingBlocks.StorageBatch);

               watch.Stop();

               inserts.Enqueue((count, size, watch.Elapsed.TotalSeconds));

               if (inserts.Count > 100)
                  inserts.Dequeue();

               int totalBlocks = inserts.Sum((tuple => tuple.Item1));
               double totalSeconds = inserts.Sum((tuple => tuple.Item3));
               double avg = totalSeconds / totalBlocks;

               log.LogDebug($"Pushed {count} blocks total Size = {(decimal)size / 1000000}mb Seconds = {watch.Elapsed.TotalSeconds} avg insert {avg:0.00} blocks per second");
            }

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
