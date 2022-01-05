using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   public abstract class TaskStarter
   {
      private readonly ILogger log;

      /// <summary>
      /// Initializes a new instance of the <see cref="TaskStarter"/> class.
      /// </summary>
      protected TaskStarter(ILogger logger)
      {
         log = logger;
      }

      public abstract int Priority { get; }

      protected Runner Runner { get; set; }

      /// <summary>
      /// The run.
      /// </summary>
      public Task Run(Runner runner, CancellationTokenSource tokenSource)
      {
         Runner = runner;
         CancellationToken cancellationToken = tokenSource.Token;

         var task = Task.Run(
             async () =>
             {
                try
                {
                   await OnExecute();

                   cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                   // do nothing the task was cancel.
                   throw;
                }
                catch (Exception ex)
                {
                   log.LogError(ex, "TaskStarter-" + GetType().Name);

                   tokenSource.Cancel();

                   throw;
                }
             },
             cancellationToken);

         return task;
      }

      public abstract Task OnExecute();
   }
}
