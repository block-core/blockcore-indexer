using System;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   public abstract class TaskRunner
   {
      private readonly ILogger log;

      /// <summary>
      /// Initializes a new instance of the <see cref="TaskRunner"/> class.
      /// </summary>
      protected TaskRunner(IOptions<IndexerSettings> configuration, ILogger logger)
      {
         log = logger;
         Delay = TimeSpan.FromSeconds(configuration.Value.SyncInterval);
      }

      public TimeSpan Delay { get; set; }

      public bool Abort { get; set; }

      protected Runner Runner { get; set; }

      protected CancellationToken CancellationToken { get; private set; }

      public Task Run(Runner runner, CancellationTokenSource tokenSource)
      {
         Runner = runner;
         CancellationToken = tokenSource.Token;

         var task = Task.Run(
             async () =>
             {

                try
                {
                   while (!Abort)
                   {
                      if (await OnExecute())
                      {
                         CancellationToken.ThrowIfCancellationRequested();

                         continue;
                      }

                      //log.LogDebug($"TaskRunner-{GetType().Name} Delay = {Delay.TotalSeconds}");

                      CancellationToken.ThrowIfCancellationRequested();

                      await Task.Delay(Delay, CancellationToken);
                   }
                }
                catch (OperationCanceledException)
                {
                   // do nothing the task was cancel.
                }
                catch (Exception ex)
                {
                   log.LogError(ex, $"TaskRunner-{GetType().Name}");

                   tokenSource.Cancel();

                   throw;
                }
             },
             CancellationToken);

         return task;
      }

      public abstract Task<bool> OnExecute();
   }
}
