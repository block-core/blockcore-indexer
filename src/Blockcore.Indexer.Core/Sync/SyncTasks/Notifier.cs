namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using System.Net.Http;
   using System.Net.Http.Formatting;
   using System.Net.Security;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;
   using Newtonsoft.Json;

   public class Notifier : TaskRunner<AddressNotifications>
   {
      private readonly ILogger<Notifier> log;

      private readonly IndexerSettings configuration;

      private readonly ChainSettings chainConfiguration;

      private readonly IStorage storage;

      private readonly Lazy<HttpClient> client;

      private readonly System.Diagnostics.Stopwatch watch;

      /// <summary>
      /// Initializes a new instance of the <see cref="Notifier"/> class. 
      /// </summary>
      public Notifier(IOptions<IndexerSettings> configuration, IOptions<ChainSettings> chainConfiguration, ILogger<Notifier> logger, IStorage storage)
          : base(configuration, logger)
      {
         this.configuration = configuration.Value;
         this.chainConfiguration = chainConfiguration.Value;
         log = logger;
         this.storage = storage;
         watch = Stopwatch.Start();
         client = new Lazy<HttpClient>(() => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) => errors == SslPolicyErrors.None || errors == SslPolicyErrors.RemoteCertificateNameMismatch }));
      }

      /// <inheritdoc />
      public override async Task<bool> OnExecute()
      {
         if (configuration.NotifyBatchCount == 0)
         {
            Abort = true;
            return true;
         }

         if (TryDequeue(out AddressNotifications item))
         {
            watch.Restart();

            var queue = new Queue<string>(item.Addresses);
            int total = queue.Count();
            int sendCount = 0;
            do
            {
               var addresses = Extensions.TakeAndRemove(queue, configuration.NotifyBatchCount).ToList();

               var coin = new CoinAddressInfo
               {
                  Symbol = chainConfiguration.Symbol,
                  Address = addresses.ToList()
               };

               try
               {
                  //await this.client.Value.PostAsync(.PostAsJsonAsync(this.configuration.NotifyUrl, coin);
                  await client.Value.PostAsync(configuration.NotifyUrl, coin, new JsonMediaTypeFormatter());
                  sendCount++;
               }
               catch (Exception ex)
               {
                  log.LogError(ex, "Notifier");
                  Abort = true;
                  return false;
               }
            }
            while (queue.Any());

            watch.Stop();

            log.LogDebug($"Seconds = {watch.Elapsed.TotalSeconds} - Total = {total} - Requests = {sendCount}");

            return true;
         }

         return false;
      }

      public class CoinAddressInfo
      {
         [JsonProperty("A")]
         public IEnumerable<string> Address { get; set; }

         [JsonProperty("C")]
         public string Symbol { get; set; }
      }
   }
}
