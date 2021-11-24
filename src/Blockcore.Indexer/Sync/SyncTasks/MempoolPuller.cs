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

   public class MempoolPuller : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<MempoolPuller> log;
      private readonly IStorageOperations storageOperations;

      private readonly System.Diagnostics.Stopwatch watch;

      bool initialized;

      /// <summary>
      /// Initializes a new instance of the <see cref="MempoolPuller"/> class.
      /// </summary>
      public MempoolPuller(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         SyncConnection syncConnection,
         ILogger<MempoolPuller> logger,
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
         if (!config.SyncMemoryPool)
         {
            Abort = true;
            return true;
         }

         if (Runner.SyncingBlocks.Blocked)
         {
            return false;
         }

         if (Runner.SyncingBlocks.ChainTipHeight == 0 ||
             Runner.SyncingBlocks.StoreTip == null ||
             Runner.SyncingBlocks.IndexModeCompleted == false ||
             Runner.SyncingBlocks.IbdMode())
         {
            // Don't sync mempool until api is at tip
            return false;
         }

         if (initialized == false)
         {
            initialized = true;

            // read build mempool memory view
            syncOperations.InitializeMmpool();
         }

         watch.Restart();

         SyncPoolTransactions pool = syncOperations.FindPoolTransactions(syncConnection, Runner.SyncingBlocks);

         if (!pool.Transactions.Any())
         {
            return false;
         }

         SyncBlockTransactionsOperation poolTrx = syncOperations.SyncPool(syncConnection, pool);

         storageOperations.InsertMempoolTransactions(poolTrx);

         watch.Stop();

         log.LogDebug($"Mempool - New Transactions = {pool.Transactions.Count}, Seconds = {watch.Elapsed.TotalSeconds}");

         return await Task.FromResult(false);
      }
   }
}
