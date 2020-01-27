namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System.Threading.Tasks;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Microsoft.Extensions.Logging;

   /// <summary>
   /// The block re-org of the block chain.
   /// </summary>
   public class BlockReorger : TaskStarter
   {
      private readonly ILogger<BlockReorger> log;

      private readonly ISyncOperations operations;

      private readonly SyncConnection connection;

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockReorger"/> class.
      /// </summary>
      public BlockReorger(ILogger<BlockReorger> logger, ISyncOperations syncOperations, SyncConnection syncConnection)
          : base(logger)
      {
         connection = syncConnection;
         operations = syncOperations;
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

      public override Task OnExecute()
      {
         log.LogDebug("Checking if re-org is required");

         return operations.CheckBlockReorganization(connection);
      }
   }
}
