using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;

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
      readonly IStorage data;
      private readonly MongoData mongoData;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockStartup"/> class.
      /// </summary>
      public BlockStartup(
         ILogger<BlockStartup> logger,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         IStorageOperations storageOperations,
         ICryptoClientFactory clientFactory,
         IStorage data)
          : base(logger)
      {
         connection = syncConnection;
         this.storageOperations = storageOperations;
         this.clientFactory = clientFactory;
         this.data = data;
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

         if (allIndexes.Count == BlockIndexer.ExpectedNumberOfIndexes)
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


            log.LogDebug($"Processing genesis hash = {genesisHash}");

            BlockInfo genesisBlock = await client.GetBlockAsync(genesisHash);
            SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(connection, genesisBlock);

            StorageBatch genesisBatch = new StorageBatch();
            storageOperations.AddToStorageBatch(genesisBatch, block);
            Runner.GlobalState.StoreTip = storageOperations.PushStorageBatch(genesisBatch);
         }

         BlockInfo fetchedBlock = await client.GetBlockAsync(Runner.GlobalState.StoreTip.BlockHash);
         if (fetchedBlock == null)
         {
            // check if the fullnode is ahead of the indexer height
            int fullnodeTipHeight = client.GetBlockCount();
            if (fullnodeTipHeight < Runner.GlobalState.StoreTip.BlockIndex)
            {
               throw new ApplicationException($"Indexer at height {fullnodeTipHeight} whihc is behind the fullnode at heigh {Runner.GlobalState.StoreTip.BlockIndex}");
            }

            // reorg happend while indexer was offline rewind the indexer database
            Runner.GlobalState.PullingTip = null;
            Runner.GlobalState.StoreTip = null;

            Runner.GlobalState.StoreTip = await syncOperations.RewindToBestChain(connection);
         }
      }
   }
}
