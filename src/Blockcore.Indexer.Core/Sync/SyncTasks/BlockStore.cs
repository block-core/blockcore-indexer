using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
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

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (Runner.GlobalState.ReorgMode)
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

         if (!TryDequeue(out StorageBatch batch))
            return await Task.FromResult(false);

         if (batch.ValidateBatch(Runner.GlobalState.StoreTip.BlockHash) == false)
            throw new ApplicationException("None consecutive block received");

         watch.Restart();

         Runner.GlobalState.StoreTip = storageOperations.PushStorageBatch(batch);

         watch.Stop();

         if (Runner.GlobalState.StoreTip == null)
            throw new ApplicationException("Store tip was not persisted");

         long totalBlocks = batch.GetBlockCount();// insertStats.Sum((tuple => tuple.count));
         double totalSeconds = watch.Elapsed.TotalSeconds;// insertStats.Sum((tuple => tuple.seconds));
         double blocksPerSecond = totalBlocks / totalSeconds;
         double secondsPerBlock = totalSeconds / totalBlocks;

         log.LogInformation($"Store - blocks={batch.GetBlockCount()}, outputs={batch.GetOutputCount()}, inputs={batch.GetInputCount()}, trx={batch.GetTransactionCount()}, total Size = {((decimal)batch.GetBatchSize() / 1000000):0.00}mb, tip={Runner.GlobalState.StoreTip.BlockIndex}, Seconds = {watch.Elapsed.TotalSeconds}, inserts = {blocksPerSecond:0.00}b/s ({secondsPerBlock:0.00}s/b)");

         foreach (var blockSiz in batch.GetBlockSizes())
            syncConnection.RecentItems.Add((DateTime.UtcNow, TimeSpan.FromSeconds(blocksPerSecond), blockSiz));

         var notifications = new AddressNotifications { Addresses = new List<string>() };// count.Items.Where(ad => ad.Addresses != null).SelectMany(s => s.Addresses).Distinct().ToList() };
         Runner.Get<Notifier>().Enqueue(notifications);

         return await Task.FromResult(true);

      }
   }
}
