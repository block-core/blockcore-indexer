using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
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

      private readonly IEnumerable<long> bip30Blocks = new List<long> {91842 , 91880 };

      BlockingCollection<SyncBlockTransactionsOperation> blockInfos = new();
      Task collectionProcessor;
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

         collectionProcessor = Task.Run(ProcessFromBlockingCollection);
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

         if (blockInfos.Count > 10000)
            return false;

         watch.Restart();

         if (Runner.GlobalState.PullingTip == null)
         {
            // start pulling blocks form this tip
            Runner.GlobalState.PullingTip = await clientFactory.Create(syncConnection)
               .GetBlockAsync(Runner.GlobalState.StoreTip.BlockHash);
            currentStorageBatch = new StorageBatch();

            log.LogDebug(
               $"Fetching block started at block {Runner.GlobalState.PullingTip.Height}({Runner.GlobalState.PullingTip.Hash})");
         }

         if (Runner.GlobalState.IbdMode())
         {
            List<Task<SyncBlockTransactionsOperation>> blocks = new (config.NumberOfPullerTasksForIBD);

            for (int i = 1; i < config.NumberOfPullerTasksForIBD + 1; i++)
            {
               blocks.Add(FetchBlockAsync(Runner.GlobalState.PullingTip.Height + i));
            }

            Task.WaitAll(blocks.ToArray());

            int index = 0;
            foreach (Task<SyncBlockTransactionsOperation> block in blocks)
            {
               string previousBlockHash = index == 0
                  ? Runner.GlobalState.PullingTip.Hash
                  : blocks[index - 1].Result.BlockInfo.Hash;

               if (block.Result.BlockInfo.PreviousBlockHash != previousBlockHash)
               {
                  log.LogDebug(
                     $"Reorg detected on block = {Runner.GlobalState.PullingTip.Height} - ({Runner.GlobalState.PullingTip.Hash})");

                  // reorgs are sorted at the store task
                  Runner.GlobalState.ReorgMode = true;
                  return false;
               }

               AddFetchedBlockToCollection(block.Result);
               index++;
            }
         }
         else
         {
            SyncBlockTransactionsOperation block = await FetchBlockAsync(Runner.GlobalState.PullingTip.Height + 1);

            if (block == null)
               return false;

            // check if the next block prev hash is the same as our current tip
            if (Runner.GlobalState.PullingTip.Hash != block.BlockInfo.PreviousBlockHash)
            {
               log.LogDebug(
                  $"Reorg detected on block = {Runner.GlobalState.PullingTip.Height} - ({Runner.GlobalState.PullingTip.Hash})");

               // reorgs are sorted at the store task
               Runner.GlobalState.ReorgMode = true;
               return false;
            }

            AddFetchedBlockToCollection(block);
         }

         watch.Stop();

         //log.LogDebug($"Puller fetched block and push to collection in{watch.Elapsed.TotalSeconds}");

         return await Task.FromResult(true);
      }

      void AddFetchedBlockToCollection(SyncBlockTransactionsOperation blockTask)
      {
         if (blockTask.BlockInfo == null)
         {
            throw new ApplicationException("Block was not found.");
         }
         else
         {
            blockInfos.Add(blockTask);

            Runner.GlobalState.PullingTip = blockTask.BlockInfo;
         }
      }

      async Task<SyncBlockTransactionsOperation> FetchBlockAsync(long tip)
      {
         var client = clientFactory.Create(syncConnection);

         // fetch the next block form the fullnode
         string nextHash = await client.GetblockHashAsync(tip);

         if (string.IsNullOrEmpty(nextHash))
         {
            // nothing to process
            return null;
         }

         // update the chains tip
         Runner.GlobalState.ChainTipHeight = syncOperations.GetBlockCount(client);

         var nextBlock = await client.GetBlockAsync(nextHash);

         return  syncOperations.FetchFullBlock(syncConnection, nextBlock);
      }


      private void ProcessFromBlockingCollection()
      {
         while (!blockInfos.IsCompleted) //TODO add cancellation token
         {
            var block = blockInfos.Take();

            storageOperations.AddToStorageBatch(currentStorageBatch, block);

            bool ibd = Runner.GlobalState.ChainTipHeight - block.BlockInfo.Height > 20;

            bool bip30Issue = bip30Blocks.Contains(block.BlockInfo.Height + 1);

            if (!ibd || bip30Issue || currentStorageBatch.BlockTable.Count >= 10000 ||
                currentStorageBatch.TotalSize > 10000000) // 5000000) // 10000000) todo: add this to config
            {
               long totalBlocks = currentStorageBatch.BlockTable.Count;
               double totalSeconds = watchBatch.Elapsed.TotalSeconds;
               double blocksPerSecond = totalBlocks / totalSeconds;
               double secondsPerBlock = totalSeconds / totalBlocks;

               log.LogDebug(
                  $"Puller - blocks={currentStorageBatch.BlockTable.Count}, height = {block.BlockInfo.Height}, batch size = {((decimal)currentStorageBatch.TotalSize / 1000000):0.00}mb, Seconds = {watchBatch.Elapsed.TotalSeconds}, fetchs = {blocksPerSecond:0.00}b/s ({secondsPerBlock:0.00}s/b). ({blockInfos.Count})");

               Runner.Get<BlockStore>().Enqueue(currentStorageBatch);
               currentStorageBatch = new StorageBatch();

               watchBatch.Restart();
            }
         }
      }
   }
}
