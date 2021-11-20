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
      readonly IUtxoCache utxoCache;

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
         SyncConnection syncConnection,
         IUtxoCache utxoCache)
          : base(configuration, logger)
      {
         this.storageOperations = storageOperations;
         this.syncOperations = syncOperations;
         this.syncConnection = syncConnection;
         this.utxoCache = utxoCache;
         log = logger;
         watch = Stopwatch.Start();
         insertStats = new Queue<(long count, long size, double seconds)>();
      }

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (Runner.SyncingBlocks.ReorgMode == true)
         {
            // null the store tip so the document count will be taken form disk
            Runner.SyncingBlocks.StoreTip = null;

            // rewind the data in store
            Runner.SyncingBlocks.StoreTip = await syncOperations.RewindToBestChain(syncConnection);
            Runner.SyncingBlocks.PullingTip = null;
            Queue.Clear();
            Runner.SyncingBlocks.ReorgMode = false;
            return false;
         }

         if (Runner.SyncingBlocks.IndexMode)
         {
            return false;
         }

         if (TryDequeue(out StorageBatch item))
         {
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

            watch.Restart();

            Runner.SyncingBlocks.StoreTip = storageOperations.PushStorageBatch(item);

            watch.Stop();

            if (Runner.SyncingBlocks.StoreTip == null)
               throw new ApplicationException("Store tip was not persisted");

            insertStats.Enqueue((item.MapBlocks.Count, item.TotalSize, watch.Elapsed.TotalSeconds));

            if (insertStats.Count > 100)
               insertStats.Dequeue();

            long totalBlocks = insertStats.Sum((tuple => tuple.count));
            double totalSeconds = insertStats.Sum((tuple => tuple.seconds));
            double avgBlocksPerSecond = totalBlocks / totalSeconds;
            double avgSecondsPerBlock = totalSeconds / totalBlocks;

            log.LogDebug($"Store - blocks={item.MapBlocks.Count}, outputs={item.AddressForOutputs.Count}, inputs={item.AddressForInputs.Count}, trx={item.MapTransactionBlocks.Count}, total Size = {((decimal)item.TotalSize / 1000000):0.00}mb, tip={Runner.SyncingBlocks.StoreTip.BlockIndex}, Seconds = {watch.Elapsed.TotalSeconds}, avg insert {avgBlocksPerSecond:0.00}b/s ({avgSecondsPerBlock:0.00}s/b)");

            foreach (MapBlock mapBlocksValue in item.MapBlocks.Values)
               syncConnection.RecentItems.Add((DateTime.UtcNow, TimeSpan.FromSeconds(avgBlocksPerSecond), mapBlocksValue.BlockSize));

            var notifications = new AddressNotifications { Addresses = new List<string>() };// count.Items.Where(ad => ad.Addresses != null).SelectMany(s => s.Addresses).Distinct().ToList() };
            Runner.Get<Notifier>().Enqueue(notifications);

            return await Task.FromResult(true);
         }

         return await Task.FromResult(false);
      }
   }
}
