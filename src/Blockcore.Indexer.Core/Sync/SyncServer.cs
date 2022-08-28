using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync
{
   /// <summary>
   /// The processes responsible of triggering sync tasks.
   /// </summary>
   public class SyncServer : IHostedService, IDisposable
   {
      private readonly IndexerSettings configuration;
      private readonly ChainSettings chainConfiguration;
      private readonly ILogger<SyncServer> log;
      private readonly IServiceScopeFactory scopeFactory;

      private Task mainTask;
      private CancellationTokenSource mainCancellationTokenSource;

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

         mainTask = Task.Run(() =>
         {
            mainCancellationTokenSource = new CancellationTokenSource();

            try
            {
               while (!mainCancellationTokenSource.IsCancellationRequested)
               {
                  var tokenSource = new CancellationTokenSource();
                  mainCancellationTokenSource.Token.Register(() =>
                  {
                     tokenSource.Cancel();
                  });

                  try
                  {
                     using (IServiceScope scope = scopeFactory.CreateScope())
                     {
                        Runner runner = scope.ServiceProvider.GetService<Runner>();
                        System.Collections.Generic.IEnumerable<Task> runningTasks = runner.RunAll(tokenSource);

                        Task.WaitAll(runningTasks.ToArray());

                        if (mainCancellationTokenSource.IsCancellationRequested)
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
                        if (innerException is OperationCanceledException)
                           throw innerException;

                        log.LogError(innerException, "Sync");
                     }

                     tokenSource.Cancel();

                     int retryInterval = 10;

                     log.LogWarning($"Unexpected error retry in {retryInterval} seconds");
                     //this.tracer.ReadLine();

                     // Blokcore Indexer is designed to be idempotent, we want to continue running even if errors are found.
                     // so if an unepxected error happened we log it wait and start again

                     Task.Delay(TimeSpan.FromSeconds(retryInterval), mainCancellationTokenSource.Token).Wait(mainCancellationTokenSource.Token);

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
            }
            catch (Exception ex)
            {
               log.LogError(ex, "Sync");
               throw;
            }
         }, cancellationToken);

         return Task.CompletedTask;
      }

      public async Task StopAsync(CancellationToken cancellationToken)
      {
         if (mainTask == null)
         {
            return;
         }

         log.LogInformation("Shutdown can take up to 20 seconds...");

         try
         {
            // Signal cancellation to the executing method
            mainCancellationTokenSource.Cancel();
         }
         finally
         {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(mainTask, Task.Delay(Timeout.Infinite, cancellationToken));
         }
      }
   }
}
