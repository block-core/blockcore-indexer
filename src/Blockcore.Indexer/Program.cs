using Blockcore.Indexer.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Indexer
{
   using Microsoft.AspNetCore.Hosting;
   using Microsoft.Extensions.Hosting;

   /// <summary>
   /// The application program.
   /// </summary>
   public class Program
   {
      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
             .ConfigureServices((hostContext, services) =>
             {
                services.Configure<HostOptions>(option =>
                {
                   // the BlockStore task can take long time to complete
                   // to avoid rewind on shutdown we allow it extra time
                   option.ShutdownTimeout = System.TimeSpan.FromSeconds(20);
                });
             })
            .ConfigureAppConfiguration(config =>
            {
               config.AddBlockcore("Blockore Indexer", args);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
               webBuilder.ConfigureKestrel(serverOptions =>
               {
                  serverOptions.AddServerHeader = false;
               });

               webBuilder.UseStartup<Startup>();
            });
   }
}
