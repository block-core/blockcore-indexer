using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
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

      readonly ICryptoClientFactory clientFactory;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockPuller"/> class.
      /// </summary>
      public BlockPuller(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         ILogger<BlockPuller> logger,
         IStorageOperations storageOperations,
         ICryptoClientFactory clientFactory)
          : base(configuration, logger)
      {
         log = logger;
         this.storageOperations = storageOperations;
         this.clientFactory = clientFactory;
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

         if (Runner.GlobalState.Blocked)
         {
            return false;
         }

         if (Runner.Get<BlockStore>().Queue.Count() >= config.MaxItemsInQueue)
         {
            return false;
         }

         if (Runner.GlobalState.ReorgMode ||
             Runner.GlobalState.IndexMode)
         {
            return false;
         }

         watch.Restart();

         BitcoinClient client = clientFactory.Create(syncConnection);

         if (Runner.GlobalState.PullingTip == null)
         {
            // start pulling blocks form this tip
            Runner.GlobalState.PullingTip = await client.GetBlockAsync(Runner.GlobalState.StoreTip.BlockHash);
            currentStorageBatch = new StorageBatch();

            log.LogDebug($"Fetching block started at block {Runner.GlobalState.PullingTip.Height}({Runner.GlobalState.PullingTip.Hash})");
         }

         // fetch the next block form the fullnode
         string nextHash = await client.GetblockHashAsync(Runner.GlobalState.PullingTip.Height + 1);

         if (string.IsNullOrEmpty(nextHash))
         {
            // nothing to process
            return false;
         }

         // update the chains tip
         Runner.GlobalState.ChainTipHeight = syncOperations.GetBlockCount(client);

         BlockInfo nextBlock = await client.GetBlockAsync(nextHash);

         // check if the next block prev hash is the same as our current tip
         if (nextBlock.PreviousBlockHash != Runner.GlobalState.PullingTip.Hash)
         {
            log.LogDebug($"Reorg detected on block = {Runner.GlobalState.PullingTip.Height} - ({Runner.GlobalState.PullingTip.Hash})");

            // reorgs are sorted at the store task
            Runner.GlobalState.ReorgMode = true;
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

         bool ibd = Runner.GlobalState.ChainTipHeight - nextBlock.Height > 20;

         if (!ibd || currentStorageBatch.BlockTable.Count >= 10000 || currentStorageBatch.TotalSize > 10000000) // 5000000) // 10000000) todo: add this to config
         {
            long totalBlocks = currentStorageBatch.BlockTable.Count;
            double totalSeconds = watchBatch.Elapsed.TotalSeconds;
            double blocksPerSecond = totalBlocks / totalSeconds;
            double secondsPerBlock = totalSeconds / totalBlocks;

            log.LogDebug($"Puller - blocks={currentStorageBatch.BlockTable.Count}, height = {nextBlock.Height}, batch size = {((decimal)currentStorageBatch.TotalSize / 1000000):0.00}mb, Seconds = {watchBatch.Elapsed.TotalSeconds}, fetchs = {blocksPerSecond:0.00}b/s ({secondsPerBlock:0.00}s/b).");

            Runner.Get<BlockStore>().Enqueue(currentStorageBatch);
            currentStorageBatch = new StorageBatch();

            watchBatch.Restart();
         }

         Runner.GlobalState.PullingTip = nextBlock;

         return await Task.FromResult(true);
      }
   }
}
