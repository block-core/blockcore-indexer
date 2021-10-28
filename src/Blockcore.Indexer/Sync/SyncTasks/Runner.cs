namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System.Collections.Concurrent;
   using System.Collections.Generic;
   using System.Linq;
   using System.Threading;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Client.Types;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations.Types;

   /// <summary>
   /// The runner.
   /// </summary>
   public class Runner
   {
      /// <summary>
      /// The task runners.
      /// </summary>
      private readonly IEnumerable<TaskRunner> taskRunners;

      /// <summary>
      /// The task starters.
      /// </summary>
      private readonly IEnumerable<TaskStarter> taskStarters;

      /// <summary>
      /// Initializes a new instance of the <see cref="Runner"/> class.
      /// </summary>
      public Runner(IEnumerable<TaskStarter> taskStarters, IEnumerable<TaskRunner> taskRunners, SyncingBlocks syncingBlocks)
      {
         this.taskStarters = taskStarters;
         this.taskRunners = taskRunners;
         SyncingBlocks = syncingBlocks;
      }

      public SyncingBlocks SyncingBlocks { get; set; }

      public T Get<T>() where T : TaskRunner
      {
         return taskRunners.OfType<T>().Single();
      }

      /// <summary>
      /// Run all tasks.
      /// </summary>
      public IEnumerable<Task> RunAll(CancellationTokenSource cancellationToken)
      {
         // execute all the starters sequentially
         taskStarters.OrderBy(o => o.Priority).ForEach(t => t.Run(this, cancellationToken).Wait());

         // execute the tasks
         return taskRunners.Select(async t => await t.Run(this, cancellationToken));
      }
   }
}
