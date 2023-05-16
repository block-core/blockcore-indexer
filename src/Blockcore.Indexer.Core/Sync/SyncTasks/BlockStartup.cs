using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   /// <summary>
   /// The block re-org of the block chain.
   /// </summary>
   public class BlockStartup : TaskStarter
   {
      private readonly ILogger<BlockStartup> log;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection connection;
      private readonly IStorageOperations storageOperations;
      readonly ICryptoClientFactory clientFactory;
      private readonly MongoData mongoData;
      readonly IOptions<IndexerSettings> indexerSettings;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockStartup"/> class.
      /// </summary>
      public BlockStartup(
         ILogger<BlockStartup> logger,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         IStorageOperations storageOperations,
         ICryptoClientFactory clientFactory,
         IStorage data,
         IOptions<IndexerSettings> indexerSettings)
          : base(logger)
      {
         connection = syncConnection;
         this.storageOperations = storageOperations;
         this.clientFactory = clientFactory;
         this.indexerSettings = indexerSettings;
         this.syncOperations = syncOperations;
         log = logger;

         mongoData = (MongoData)data;

      }

      /// <summary>
      /// Gets the priority.
      /// </summary>
      public override int Priority
      {
         get
         {
            return 50;
         }
      }

      public override async Task OnExecute()
      {
         IBlockchainClient client = clientFactory.Create(connection);

         List<string> allIndexes = mongoData.GetBlockIndexIndexes();

         if (allIndexes.Count == indexerSettings.Value.IndexCountForBlockIndexProperty)
         {
            Runner.GlobalState.IndexModeCompleted = true;
         }

         Runner.GlobalState.PullingTip = null;
         Runner.GlobalState.StoreTip = null;

         Runner.GlobalState.StoreTip = await syncOperations.RewindToLastCompletedBlockAsync();

         if (Runner.GlobalState.StoreTip == null)
         {
            // No blocks in store start from zero
            // push the genesis block to store
            int start = 0;
            string genesisHash = await client.GetblockHashAsync(start);


            log.LogInformation($"Processing genesis hash = {genesisHash}");

            BlockInfo genesisBlock = await client.GetBlockAsync(genesisHash);
            SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(connection, genesisBlock);

            StorageBatch genesisBatch = new StorageBatch();
            storageOperations.AddToStorageBatch(genesisBatch, block);
            Runner.GlobalState.StoreTip = storageOperations.PushStorageBatch(genesisBatch);
         }

         BlockInfo fetchedBlock = await GetBlockOrNullAsync(client, Runner.GlobalState.StoreTip.BlockHash);
         if (fetchedBlock == null)
         {
            // check if the fullnode is ahead of the indexer height
            int fullnodeTipHeight = client.GetBlockCount();
            if (fullnodeTipHeight < Runner.GlobalState.StoreTip.BlockIndex)
            {
               throw new ApplicationException($"Full node at height {fullnodeTipHeight} which is behind the Indexer at height {Runner.GlobalState.StoreTip.BlockIndex}");
            }

            // reorg happend while indexer was offline rewind the indexer database
            Runner.GlobalState.PullingTip = null;
            Runner.GlobalState.StoreTip = null;

            Runner.GlobalState.StoreTip = await syncOperations.RewindToBestChain(connection);
         }

         // update the chains tip
         Runner.GlobalState.ChainTipHeight = syncOperations.GetBlockCount(client);
      }

      private static async Task<BlockInfo> GetBlockOrNullAsync(IBlockchainClient client, string blockHash)
      {
         try
         {
            BlockInfo blockInfo = await client.GetBlockAsync(blockHash);

            return blockInfo;
         }
         catch (Exception e)
         {
            if (e.Message.Contains("Block not found"))
            {
               return null;
            }

            throw;
         }
      }
   }
}
