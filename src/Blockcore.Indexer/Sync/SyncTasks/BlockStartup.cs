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

         Runner.SyncingBlocks.PullingTip = null;
         Runner.SyncingBlocks.StoreTip = null;

         Runner.SyncingBlocks.StoreTip = syncOperations.RewindToLastCompletedBlock();

         if (Runner.SyncingBlocks.StoreTip == null)
         {
            // No blocks in store start from zero
            // push the genesis block to store
            string genesisHash = client.GetblockHash(0);

            log.LogDebug($"Processing genesis hash = {genesisHash}");

            BlockInfo genesisBlock = await client.GetBlockAsync(genesisHash);
            SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(connection, genesisBlock);

            StorageBatch genesisBatch = new StorageBatch();
            storageOperations.AddToStorageBatch(genesisBatch, block);
            Runner.SyncingBlocks.StoreTip = storageOperations.PushStorageBatch(genesisBatch);
         }

         BlockInfo fetchedBlock = await client.GetBlockAsync(Runner.SyncingBlocks.StoreTip.BlockHash);
         if (fetchedBlock == null)
         {
            Runner.SyncingBlocks.StoreTip = await syncOperations.RewindToBestChain(connection);
         }
      }
   }
}
