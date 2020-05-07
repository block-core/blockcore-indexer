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

   public class PoolFinder : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<PoolFinder> log;

      private readonly System.Diagnostics.Stopwatch watch;

      /// <summary>
      /// Initializes a new instance of the <see cref="PoolFinder"/> class.
      /// </summary>
      public PoolFinder(IOptions<IndexerSettings> configuration, ISyncOperations syncOperations, SyncConnection syncConnection, ILogger<PoolFinder> logger)
          : base(configuration, logger)
      {
         log = logger;
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

         SyncingBlocks syncingBlocks = Runner.SyncingBlocks;

         if (syncingBlocks.Blocked)
         {
            return false;
         }

         if (syncingBlocks.LastBlock == null || syncingBlocks.LastBlock.Height + 10 < syncingBlocks.LastClientBlockIndex)
         {
            // Don't sync mempool until api is at tip
            return false;
         }

         if (Runner.Get<BlockSyncer>().Queue.Count() >= config.MaxItemsInQueue)
         {
            return false;
         }

         watch.Restart();

         SyncPoolTransactions pool = syncOperations.FindPoolTransactions(syncConnection, syncingBlocks);

         if (!pool.Transactions.Any())
         {
            return false;
         }

         watch.Stop();

         log.LogDebug($"Seconds = {watch.Elapsed.TotalSeconds} - New Transactions = {pool.Transactions.Count}/{syncingBlocks.CurrentPoolSyncing.Count()}");

         Runner.Get<BlockSyncer>().Enqueue(new SyncBlockOperation { PoolTransactions = pool });

         return await Task.FromResult(false);
      }
   }
}
