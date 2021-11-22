using System;
using Blockcore.Indexer.Client;
using Blockcore.Indexer.Client.Types;

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
   public class BlockPuller : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<BlockPuller> log;

      private readonly IStorageOperations storageOperations;

      private readonly System.Diagnostics.Stopwatch watch;

      private readonly System.Diagnostics.Stopwatch watchBatch;

      private StorageBatch currentStorageBatch;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockPuller"/> class.
      /// </summary>
      public BlockPuller(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         ILogger<BlockPuller> logger,
         IStorageOperations storageOperations)
          : base(configuration, logger)
      {
         log = logger;
         this.storageOperations = storageOperations;
         this.syncConnection = syncConnection;
         this.syncOperations = syncOperations;
         config = configuration.Value;
         watch = Stopwatch.Start();
         watchBatch = Stopwatch.Start();
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

         if (Runner.Get<BlockStore>().Queue.Count() >= config.MaxItemsInQueue)
         {
            return false;
         }

         if (Runner.SyncingBlocks.ReorgMode ||
             Runner.SyncingBlocks.IndexMode)
         {
            return false;
         }

         watch.Restart();

         BitcoinClient client = CryptoClientFactory.Create(syncConnection);

         if (Runner.SyncingBlocks.PullingTip == null)
         {
            // start pulling blocks form this tip
            Runner.SyncingBlocks.PullingTip = await client.GetBlockAsync(Runner.SyncingBlocks.StoreTip.BlockHash);
            currentStorageBatch = new StorageBatch();

            log.LogDebug($"Fetching block started at block {Runner.SyncingBlocks.PullingTip.Height}({Runner.SyncingBlocks.PullingTip.Hash})");
         }

         // fetch the next block form the fullnode
         string nextHash = await client.GetblockHashAsync(Runner.SyncingBlocks.PullingTip.Height + 1);

         if (string.IsNullOrEmpty(nextHash))
         {
            // nothing to process
            return false;
         }

         // update the chains tip
         Runner.SyncingBlocks.ChainTipHeight = syncOperations.GetBlockCount(client);

         BlockInfo nextBlock = await client.GetBlockAsync(nextHash);

         // check if the next block prev hash is the same as our current tip
         if (nextBlock.PreviousBlockHash != Runner.SyncingBlocks.PullingTip.Hash)
         {
            log.LogDebug($"Reorg detected on block = {Runner.SyncingBlocks.PullingTip.Height} - ({Runner.SyncingBlocks.PullingTip.Hash})");

            // reorgs are sorted at the store task
            Runner.SyncingBlocks.ReorgMode = true;
            return false;
         }

         // build mongod data from that block
         SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(syncConnection, nextBlock);

         if (block?.BlockInfo == null)
         {
            throw new ApplicationException("Block was not found.");
         }

         storageOperations.AddToStorageBatch(currentStorageBatch, block);

         watch.Stop();

         bool ibd = Runner.SyncingBlocks.ChainTipHeight - nextBlock.Height > 20;

         if (!ibd || currentStorageBatch.MapBlocks.Count >= 10000 || currentStorageBatch.TotalSize > 10000000) // 5000000) // 10000000) todo: add this to config
         {
            long totalBlocks = currentStorageBatch.MapBlocks.Count;
            double totalSeconds = watchBatch.Elapsed.TotalSeconds;
            double blocksPerSecond = totalBlocks / totalSeconds;
            double secondsPerBlock = totalSeconds / totalBlocks;

            log.LogDebug($"Puller - blocks={currentStorageBatch.MapBlocks.Count}, height = {nextBlock.Height}, batch size = {((decimal)currentStorageBatch.TotalSize / 1000000):0.00}mb, Seconds = {watchBatch.Elapsed.TotalSeconds}, fetchs = {blocksPerSecond:0.00}b/s ({secondsPerBlock:0.00}s/b).");

            Runner.Get<BlockStore>().Enqueue(currentStorageBatch);
            currentStorageBatch = new StorageBatch();

            watchBatch.Restart();
         }

         Runner.SyncingBlocks.PullingTip = nextBlock;

         return await Task.FromResult(true);
      }
   }
}
