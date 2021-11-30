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
      }

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (Runner.GlobalState.ReorgMode == true)
         {
            // null the store tip so the document count will be taken form disk
            Runner.GlobalState.StoreTip = null;

            // rewind the data in store
            Runner.GlobalState.StoreTip = await syncOperations.RewindToBestChain(syncConnection);
            Runner.GlobalState.PullingTip = null;
            Queue.Clear();
            Runner.GlobalState.ReorgMode = false;
            return false;
         }

         if (Runner.GlobalState.IndexMode)
         {
            return false;
         }

         if (TryDequeue(out StorageBatch item))
         {
            // check all blocks are consecutive and start from the last block in store.
            string prevHash = Runner.GlobalState.StoreTip.BlockHash;
            foreach (BlockTable mapBlock in item.BlockTable.Values.OrderBy(b => b.BlockIndex))
            {
               if (mapBlock.PreviousBlockHash != prevHash)
               {
                  throw new ApplicationException("None consecutive block received");
               }

               prevHash = mapBlock.BlockHash;
            }

            watch.Restart();

            Runner.GlobalState.StoreTip = storageOperations.PushStorageBatch(item);

            watch.Stop();

            if (Runner.GlobalState.StoreTip == null)
               throw new ApplicationException("Store tip was not persisted");

            long totalBlocks = item.BlockTable.Count;// insertStats.Sum((tuple => tuple.count));
            double totalSeconds = watch.Elapsed.TotalSeconds;// insertStats.Sum((tuple => tuple.seconds));
            double blocksPerSecond = totalBlocks / totalSeconds;
            double secondsPerBlock = totalSeconds / totalBlocks;

            log.LogDebug($"Store - blocks={item.BlockTable.Count}, outputs={item.OutputTable.Count}, inputs={item.InputTable.Count}, trx={item.TransactionBlockTable.Count}, total Size = {((decimal)item.TotalSize / 1000000):0.00}mb, tip={Runner.GlobalState.StoreTip.BlockIndex}, Seconds = {watch.Elapsed.TotalSeconds}, inserts = {blocksPerSecond:0.00}b/s ({secondsPerBlock:0.00}s/b)");

            foreach (BlockTable mapBlocksValue in item.BlockTable.Values)
               syncConnection.RecentItems.Add((DateTime.UtcNow, TimeSpan.FromSeconds(blocksPerSecond), mapBlocksValue.BlockSize));

            var notifications = new AddressNotifications { Addresses = new List<string>() };// count.Items.Where(ad => ad.Addresses != null).SelectMany(s => s.Addresses).Distinct().ToList() };
            Runner.Get<Notifier>().Enqueue(notifications);

            return await Task.FromResult(true);
         }

         return await Task.FromResult(false);
      }
   }
}
