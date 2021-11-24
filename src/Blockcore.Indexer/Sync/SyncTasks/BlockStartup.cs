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
            Runner.GlobalState.StoreTip = await syncOperations.RewindToBestChain(connection);
         }
      }
   }
}
