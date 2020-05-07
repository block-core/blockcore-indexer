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

   /// <summary>
   /// The block sync.
   /// </summary>
   public class BlockFinder : TaskRunner
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<BlockFinder> log;

      private readonly System.Diagnostics.Stopwatch watch;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockFinder"/> class.
      /// </summary>
      public BlockFinder(IOptions<IndexerSettings> configuration, ISyncOperations syncOperations, SyncConnection syncConnection, ILogger<BlockFinder> logger)
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
         if (!config.SyncBlockchain)
         {
            Abort = true;
            return true;
         }

         SyncingBlocks syncingBlocks = Runner.SyncingBlocks;

         if (syncingBlocks.Blocked)
         {
            return false;
         }

         if (Runner.Get<BlockSyncer>().Queue.Count() >= config.MaxItemsInQueue)
         {
            return false;
         }

         watch.Restart();

         SyncBlockOperation block = syncOperations.FindBlock(syncConnection, syncingBlocks);

         if (block == null || block.BlockInfo == null)
         {
            return false;
         }

         watch.Stop();

         string blockStatus = block.IncompleteBlock ? "Incomplete" : string.Empty;
         log.LogDebug($"Seconds = {watch.Elapsed.TotalSeconds} - SyncedIndex = {block.BlockInfo.Height}/{block.LastCryptoBlockIndex} - {block.LastCryptoBlockIndex - block.BlockInfo.Height} {blockStatus}");

         Runner.Get<BlockSyncer>().Enqueue(block);

         return await Task.FromResult(true);
      }
   }
}
