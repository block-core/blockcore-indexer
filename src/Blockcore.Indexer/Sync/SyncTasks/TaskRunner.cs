namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System;
   using System.Threading;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Settings;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

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

      public Task Run(Runner runner, CancellationTokenSource tokenSource)
      {
         Runner = runner;
         CancellationToken cancellationToken = tokenSource.Token;

         var task = Task.Run(
             async () =>
             {
                try
                {
                   while (!Abort)
                   {
                      if (await OnExecute())
                      {
                         cancellationToken.ThrowIfCancellationRequested();

                         continue;
                      }

                      log.LogDebug($"TaskRunner-{GetType().Name} Delay = {Delay.TotalSeconds}");

                      cancellationToken.ThrowIfCancellationRequested();

                      await Task.Delay(Delay, cancellationToken);
                   }
                }
                catch (OperationCanceledException)
                {
                   // do nothing the task was cancel.
                   throw;
                }
                catch (Exception ex)
                {
                   log.LogError(ex, $"TaskRunner-{GetType().Name}");

                   tokenSource.Cancel();

                   throw;
                }
             },
             cancellationToken);

         return task;
      }

      public abstract Task<bool> OnExecute();
   }
}