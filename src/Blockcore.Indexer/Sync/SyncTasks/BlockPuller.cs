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
      }

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (!config.SyncBlockchain)
         {
            Abort = true;
            return true;
         }

         SyncingBlocks syncingBlocks = Runner.SyncingBlocks;

         if (syncingBlocks.Blocked)
         {
            return false;
         }

         if (Runner.Get<BlockStore>().Queue.Count() >= config.MaxItemsInQueue)
         {
            return false;
         }

         if (Runner.SyncingBlocks.ReorgMode)
         {
            return false;
         }

         watch.Restart();

         BitcoinClient client = CryptoClientFactory.Create(syncConnection);

         if (Runner.SyncingBlocks.PullingTip == null)
         {
            Runner.SyncingBlocks.PullingTip = await client.GetBlockAsync(Runner.SyncingBlocks.StoreTip.BlockHash);
         }

         // fetch the next block form the fullnode
         string nextHash = await client.GetblockHashAsync(Runner.SyncingBlocks.PullingTip.Height + 1);

         if (string.IsNullOrEmpty(nextHash))
         {
            // nothing to process
            return false;
         }

         BlockInfo nextBlock = await client.GetBlockAsync(nextHash);

         // check if the next block prev hash is the same as our current tip
         if (nextBlock.PreviousBlockHash != Runner.SyncingBlocks.PullingTip.Hash)
         {
            log.LogDebug($"Reorg detected on block = {Runner.SyncingBlocks.PullingTip.Height} - ({Runner.SyncingBlocks.PullingTip.Hash})");

            Runner.SyncingBlocks.ReorgMode = true;
            return false;
         }

         // build mongod data from that block
         SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(syncConnection, nextBlock);

         if (block == null || block.BlockInfo == null)
         {
            throw new ApplicationException("Block was not found.");
         }

         watch.Stop();

         log.LogDebug($"Fetched block = {nextBlock.Height}({nextHash}) Transactions = {nextBlock.Transactions.Count()} Size = {(decimal)nextBlock.Size / 1000000}mb Seconds = {watch.Elapsed.TotalSeconds}");

         //log.LogDebug($"Seconds = {watch.Elapsed.TotalSeconds} - SyncedIndex = {block.BlockInfo.Height}/{Runner.SyncingBlocks.LastClientBlockIndex} - {Runner.SyncingBlocks.LastClientBlockIndex - block.BlockInfo.Height}");

         Runner.Get<BlockStore>().Enqueue(block);

         Runner.SyncingBlocks.PullingTip = nextBlock;

         return await Task.FromResult(true);
      }
   }
}
