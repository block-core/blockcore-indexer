namespace Blockcore.Indexer.Sync
{
   using System;
   using System.Linq;
   using System.Threading;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Sync.SyncTasks;
   using Microsoft.Extensions.DependencyInjection;
   using Microsoft.Extensions.Hosting;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

   /// <summary>
   /// The processes responsible of triggering sync tasks.
   /// </summary>
   public class SyncServer : IHostedService, IDisposable
   {
      private readonly IndexerSettings configuration;
      private readonly ChainSettings chainConfiguration;
      private readonly ILogger<SyncServer> log;
      private readonly IServiceScopeFactory scopeFactory;

      /// <summary>
      /// Initializes a new instance of the <see cref="SyncServer"/> class.
      /// </summary>
      public SyncServer(ILogger<SyncServer> logger, IOptions<IndexerSettings> configuration, IOptions<ChainSettings> chainConfiguration, IServiceScopeFactory scopeFactory)
      {
         log = logger;
         this.configuration = configuration.Value;
         this.chainConfiguration = chainConfiguration.Value;
         this.scopeFactory = scopeFactory;
      }

      public void Dispose()
      {

      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         log.LogInformation($"Start sync for {chainConfiguration.Symbol}");
         log.LogInformation("Starting the Sync Service...");

         Task.Run(async () =>
         {
            try
            {
               while (!cancellationToken.IsCancellationRequested)
               {
                  var tokenSource = new CancellationTokenSource();
                  cancellationToken.Register(() => { tokenSource.Cancel(); });

                  try
                  {
                     using (IServiceScope scope = scopeFactory.CreateScope())
                     {
                        Runner runner = scope.ServiceProvider.GetService<Runner>();
                        System.Collections.Generic.IEnumerable<Task> runningTasks = runner.RunAll(tokenSource);

                        Task.WaitAll(runningTasks.ToArray(), cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                        {
                           tokenSource.Cancel();
                        }
                     }

                     break;
                  }
                  catch (OperationCanceledException)
                  {
                     // do nothing the task was cancel.
                     throw;
                  }
                  catch (AggregateException ae)
                  {
                     if (ae.Flatten().InnerExceptions.OfType<SyncRestartException>().Any())
                     {
                        log.LogInformation("Sync: ### - Restart requested - ###");
                        log.LogTrace("Sync: Signalling token cancelation");
                        tokenSource.Cancel();

                        continue;
                     }

                     foreach (Exception innerException in ae.Flatten().InnerExceptions)
                     {
                        log.LogError(innerException, "Sync");
                     }

                     tokenSource.Cancel();

                     int retryInterval = 10;

                     log.LogWarning($"Unexpected error retry in {retryInterval} seconds");
                     //this.tracer.ReadLine();

                     // Blokcore Indexer is designed to be idempotent, we want to continue running even if errors are found.
                     // so if an unepxected error happened we log it wait and start again

                     Task.Delay(TimeSpan.FromSeconds(retryInterval), cancellationToken).Wait(cancellationToken);

                     continue;
                  }
                  catch (Exception ex)
                  {
                     log.LogError(ex, "Sync");
                     break;
                  }
               }
            }
            catch (OperationCanceledException)
            {
               // do nothing the task was cancel.
               throw;
            }
            catch (Exception ex)
            {
               log.LogError(ex, "Sync");
               throw;
            }

         }, cancellationToken);
         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }
   }
}
