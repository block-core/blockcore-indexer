using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
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

      private readonly System.Diagnostics.Stopwatch watchBatch;

      private StorageBatch currentStorageBatch;

      readonly ICryptoClientFactory clientFactory;

      readonly IStorageBatchFactory StorageBatchFactory;

      private readonly IEnumerable<long> bip30Blocks = new List<long> {91842 , 91880 };

      private readonly BlockingCollection<SyncBlockTransactionsOperation> pendingBlocksToAddToStorage = new();

      private readonly Task collectionProcessor;

      private bool drainBlockingCollection = false;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockPuller"/> class.
      /// </summary>
      public BlockPuller(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         ILogger<BlockPuller> logger,
         IStorageOperations storageOperations,
         ICryptoClientFactory clientFactory,
         IStorageBatchFactory storageBatchFactory)
          : base(configuration, logger)
      {
         log = logger;
         this.storageOperations = storageOperations;
         this.clientFactory = clientFactory;
         StorageBatchFactory = storageBatchFactory;
         this.syncConnection = syncConnection;
         this.syncOperations = syncOperations;
         config = configuration.Value;
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

         if (drainBlockingCollection)
         {
            if (pendingBlocksToAddToStorage.Count == 0)
               drainBlockingCollection = false;
            else
               return false;
         }

         if (pendingBlocksToAddToStorage.Count > config.MaxItemsInBlockingCollection)
         {
            return false;
         }

         if (Runner.GlobalState.PullingTip == null)
         {
            // start pulling blocks form this tip
            Runner.GlobalState.PullingTip = await clientFactory.Create(syncConnection).GetBlockAsync(Runner.GlobalState.StoreTip.BlockHash);
            currentStorageBatch = StorageBatchFactory.GetStorageBatch();

            log.LogInformation($"Fetching block started at block {Runner.GlobalState.PullingTip.Height}({Runner.GlobalState.PullingTip.Hash})");
         }

         if (collectionProcessor.IsFaulted)
         {
            throw collectionProcessor.Exception;
         }

         // update the chains tip
         Runner.GlobalState.ChainTipHeight = syncOperations.GetBlockCount(clientFactory.Create(syncConnection));

         if (Runner.GlobalState.ChainTipHeight == Runner.GlobalState.PullingTip.Height)
            return false;

         int numberOfTasks = Runner.GlobalState.IbdMode() ? config.NumberOfPullerTasksForIBD : 1;

         List<Task<SyncBlockTransactionsOperation>> blocks = new(numberOfTasks);

         for (int i = 1; i < numberOfTasks + 1; i++)
            blocks.Add(FetchBlockAsync(Runner.GlobalState.PullingTip.Height + i));

         Task.WaitAll(blocks.ToArray());

         int index = 0;
         foreach (Task<SyncBlockTransactionsOperation> block in blocks)
         {
            if (block.Result == null)
               return false; //we are at the tip

            string previousBlockHash = index == 0
               ? Runner.GlobalState.PullingTip.Hash
               : blocks[index - 1].Result.BlockInfo.Hash;

            if (block.Result.BlockInfo.PreviousBlockHash != previousBlockHash)
            {
               log.LogInformation($"Reorg detected on block = {Runner.GlobalState.PullingTip.Height} - ({Runner.GlobalState.PullingTip.Hash})");

               // reorgs are sorted at the store task
               drainBlockingCollection = true;
               Runner.GlobalState.ReorgMode = true;
               return false;
            }

            AddFetchedBlockToCollection(block.Result);
            index++;
         }

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
            pendingBlocksToAddToStorage.Add(blockTask);

            Runner.GlobalState.PullingTip = blockTask.BlockInfo;
         }
      }

      async Task<SyncBlockTransactionsOperation> FetchBlockAsync(long tip)
      {
         IBlockchainClient client = clientFactory.Create(syncConnection);

         // fetch the next block form the fullnode
         string nextHash = await NextHashAsync(client, tip);

         if (string.IsNullOrEmpty(nextHash))
         {
            // nothing to process
            return null;
         }

         BlockInfo nextBlock = await client.GetBlockAsync(nextHash);

         return  syncOperations.FetchFullBlock(syncConnection, nextBlock);
      }

      private void ProcessFromBlockingCollection()
      {
         while (!pendingBlocksToAddToStorage.IsCompleted || !Abort)
         {
            if (drainBlockingCollection)
            {
               while (pendingBlocksToAddToStorage.TryTake(out _)) { }
               drainBlockingCollection = false;
            }

            SyncBlockTransactionsOperation block = pendingBlocksToAddToStorage.Take(CancellationToken);

            storageOperations.AddToStorageBatch(currentStorageBatch, block);

            bool ibd = Runner.GlobalState.ChainTipHeight - block.BlockInfo.Height > 20;

            bool bip30Issue = bip30Blocks.Contains(block.BlockInfo.Height + 1);

            if (ibd && !bip30Issue &&
                currentStorageBatch.GetBlockCount() < config.DbBatchCount &&
                currentStorageBatch.GetBatchSize() <= config.DbBatchSize)
            {
               continue;
            }

            long totalBlocks = currentStorageBatch.GetBlockCount();
            double totalSeconds = watchBatch.Elapsed.TotalSeconds;
            double blocksPerSecond = totalBlocks / totalSeconds;
            double secondsPerBlock = totalSeconds / totalBlocks;

            log.LogInformation($"Puller - blocks={currentStorageBatch.GetBlockCount()}, height = {block.BlockInfo.Height}, batch size = {((decimal)currentStorageBatch.GetBatchSize() / 1000000):0.00}mb, Seconds = {watchBatch.Elapsed.TotalSeconds}, fetchs = {blocksPerSecond:0.00}b/s ({secondsPerBlock:0.00}s/b). ({pendingBlocksToAddToStorage.Count})");

            Runner.Get<BlockStore>().Enqueue(currentStorageBatch);
            currentStorageBatch = StorageBatchFactory.GetStorageBatch();

            watchBatch.Restart();
         }
      }

      private static async Task<string> NextHashAsync(IBlockchainClient client, long height)
      {
         try
         {
            string blockHash = await client.GetblockHashAsync(height);

            return blockHash;
         }
         catch (Exception e)
         {
            if (e.Message.Contains("Block height out of range"))
            {
               return null;
            }

            throw;
         }
      }
   }
}
