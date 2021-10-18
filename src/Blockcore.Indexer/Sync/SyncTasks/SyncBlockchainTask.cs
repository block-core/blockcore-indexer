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
   using Blockcore.Indexer.Client;
   using Blockcore.Indexer.Storage;
   using Blockcore.Indexer.Client.Types;

   /// <summary>
   /// The block sync.
   /// </summary>
   public class SyncBlockchainTask : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<BlockFinder> log;

      private readonly System.Diagnostics.Stopwatch watch;

      private readonly IStorage storage;
      private readonly IStorageOperations storageOperations;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockFinder"/> class.
      /// </summary>
      public SyncBlockchainTask(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         ILogger<BlockFinder> logger,
         IStorage storage,
         IStorageOperations storageOperations)
          : base(configuration, logger)
      {
         log = logger;
         this.storage = storage;
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

         SyncConnection connection = syncConnection;
         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         Storage.Types.SyncBlockInfo tip = RewindToLastCompletedBlock();

         if (tip == null)
         {
            // No blocks in store start from zero
            // push the genesis block to store
            string genesisHash = client.GetblockHash(0);
            BlockInfo genesisBlock = client.GetBlock(genesisHash);

            log.LogDebug($"Processing genesis hash = {genesisHash}");

            SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(syncConnection, genesisBlock);

            // todo: unify this later
            storageOperations.ValidateBlock(block);
            InsertStats count = storageOperations.InsertTransactions(block);

            tip = storage.BlockByHash(genesisHash);
         }

         while (config.SyncBlockchain)
         {
            watch.Restart();

            // fetch the next block form the fullnode
            string nextHash = client.GetblockHash(tip.BlockIndex + 1);

            if (string.IsNullOrEmpty(nextHash))
            {
               // nothing to process
               break;
            }

            BlockInfo nextBlock = client.GetBlock(nextHash);

            // check if the next block prev hash is the same as our current tip
            if (nextBlock.PreviousBlockHash != tip.BlockHash)
            {
               // todo: implement reorg
               // reorg, delete the last block and go back one block then restart the loop
               log.LogDebug($"Reorg detected on block = {tip.BlockIndex}");
               // await syncOperations.CheckBlockReorganization(connection);

               storage.DeleteBlock(tip.BlockHash);
               tip = storage.BlockByIndex(tip.BlockIndex - 1);
               continue;
            }

            // build mongod data from that block
            SyncBlockTransactionsOperation block = syncOperations.FetchFullBlock(syncConnection, nextBlock);

            storageOperations.ValidateBlock(block);
            InsertStats count = storageOperations.InsertTransactions(block);

            tip = storage.BlockByHash(nextHash);

            watch.Stop();

            log.LogDebug($"Processed block = {nextBlock.Height}({nextHash}) Transactions = {nextBlock.Transactions.Count()} Size = {(decimal)nextBlock.Size / 1000000}mb Seconds = {watch.Elapsed.TotalSeconds}");
         }

         return await Task.FromResult(true);
      }

      public Storage.Types.SyncBlockInfo RewindToLastCompletedBlock()
      {
         Storage.Types.SyncBlockInfo lastBlock = storage.GetLatestBlock();

         if (lastBlock == null)
            return null;

         while (lastBlock != null && lastBlock.SyncComplete == false)
         {
            storage.DeleteBlock(lastBlock.BlockHash);
            lastBlock = storage.BlockByIndex(lastBlock.BlockIndex - 1);
         }

         return lastBlock;
      }
   }
}
