using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
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

         if (Runner.GlobalState.Blocked)
         {
            return false;
         }

         if (Runner.GlobalState.ChainTipHeight == 0 ||
             Runner.GlobalState.StoreTip == null ||
             Runner.GlobalState.IndexModeCompleted == false ||
             Runner.GlobalState.IbdMode())
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

         SyncPoolTransactions pool = syncOperations.FindPoolTransactions(syncConnection);

         if (!pool.Transactions.Any())
         {
            return false;
         }

         SyncBlockTransactionsOperation poolTrx = syncOperations.SyncPool(syncConnection, pool);

         storageOperations.InsertMempoolTransactions(poolTrx);

         watch.Stop();

         log.LogInformation($"Mempool - New Transactions = {pool.Transactions.Count}, Seconds = {watch.Elapsed.TotalSeconds}");

         return await Task.FromResult(false);
      }
   }
}
