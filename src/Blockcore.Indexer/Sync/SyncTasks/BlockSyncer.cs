namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System.Linq;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Client;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

   /// <summary>
   /// The block sync.
   /// </summary>
   public class BlockSyncer : TaskRunner<SyncBlockOperation>
   {
      private readonly IndexerSettings config;

      private readonly ISyncOperations syncOperations;

      private readonly SyncConnection syncConnection;

      private readonly ILogger<BlockSyncer> log;

      private readonly System.Diagnostics.Stopwatch watch;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockSyncer"/> class.
      /// </summary>
      public BlockSyncer(IOptions<IndexerSettings> configuration, ISyncOperations syncOperations, SyncConnection syncConnection, ILogger<BlockSyncer> logger)
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
         if (Runner.Get<BlockStore>().Queue.Count() >= config.MaxItemsInQueue)
         {
            return false;
         }


         if (TryDequeue(out SyncBlockOperation item))
         {
            watch.Restart();

            try
            {
               if (item.BlockInfo != null)
               {
                  SyncBlockTransactionsOperation block = syncOperations.SyncBlock(syncConnection, item.BlockInfo);

                  int inputs = block.Transactions.SelectMany(s => s.Inputs).Count();
                  int outputs = block.Transactions.SelectMany(s => s.Outputs).Count();

                  Runner.Get<BlockStore>().Enqueue(block);

                  log.LogDebug($"Seconds = {watch.Elapsed.TotalSeconds} - BlockIndex = {block.BlockInfo.Height} - Transactions {block.Transactions.Count()} - Inputs {inputs} - Outputs {outputs} - ({inputs + outputs})");
               }

               if (item.PoolTransactions != null)
               {
                  SyncBlockTransactionsOperation pool = syncOperations.SyncPool(syncConnection, item.PoolTransactions);

                  int inputs = pool.Transactions.SelectMany(s => s.Inputs).Count();
                  int outputs = pool.Transactions.SelectMany(s => s.Outputs).Count();

                  Runner.Get<BlockStore>().Enqueue(pool);

                  log.LogDebug($"Seconds = {watch.Elapsed.TotalSeconds} - Pool = Sync - Transactions {pool.Transactions.Count()} - Inputs {inputs} - Outputs {outputs} - ({inputs + outputs})");
               }
            }
            catch (BitcoinClientException bce)
            {
               if (bce.ErrorCode == -5)
               {
                  if (bce.ErrorMessage == "No information available about transaction")
                  {
                     throw new SyncRestartException(string.Empty, bce);
                  }
               }

               throw;
            }

            watch.Stop();

            return true;
         }

         return await Task.FromResult(false);
      }
   }
}
