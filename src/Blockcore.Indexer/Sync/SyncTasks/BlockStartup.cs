using Blockcore.Indexer.Client.Types;

namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System.Threading.Tasks;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Microsoft.Extensions.Logging;

   /// <summary>
   /// The block re-org of the block chain.
   /// </summary>
   public class BlockStartup : TaskStarter
   {
      private readonly ILogger<BlockStartup> log;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection connection;
      private readonly IStorageOperations storageOperations;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockStartup"/> class.
      /// </summary>
      public BlockStartup(
         ILogger<BlockStartup> logger,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         IStorageOperations storageOperations)
          : base(logger)
      {
         connection = syncConnection;
         this.storageOperations = storageOperations;
         this.syncOperations = syncOperations;
         log = logger;
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
         Client.BitcoinClient client = Client.CryptoClientFactory.Create(connection);

         // null the pulling tip so it will start form store tip
         Runner.SyncingBlocks.PullingTip = null;

         // null the store tip so the document count will be taken form disk
         Runner.SyncingBlocks.StoreTip = null;

         // rewind the chain down to the last block that completed to sync
         Runner.SyncingBlocks.StoreTip = syncOperations.RewindToLastCompletedBlock();

         if (Runner.SyncingBlocks.StoreTip == null)
         {
            // No blocks in store start from zero
            // push the genesis block to store
            string startHash = client.GetblockHash(connection.StartBlockIndex);

            log.LogDebug($"Processing the first block hash = {startHash}");

            BlockInfo startBlock = await client.GetBlockAsync(startHash);
            SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(connection, startBlock);

            StorageBatch startBlockBatch = new StorageBatch();
            storageOperations.AddToStorageBatch(startBlockBatch, block);
            Runner.SyncingBlocks.StoreTip = storageOperations.PushStorageBatch(startBlockBatch);
         }

         // a reorg happened when the node was shutdown reorg the store to the best known block
         BlockInfo fetchedBlock = await client.GetBlockAsync(Runner.SyncingBlocks.StoreTip.BlockHash);
         if (fetchedBlock == null)
         {
            Runner.SyncingBlocks.StoreTip = await syncOperations.RewindToBestChain(connection);
         }
      }
   }
}
