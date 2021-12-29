using Blockcore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cirrus
{

   using Microsoft.AspNetCore.Hosting;
   using Microsoft.Extensions.Hosting;


   public class Program //: Blockcore.Indexer.Program
   {
      public static new void Main(string[] args)
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

               webBuilder.UseStartup<CirrusStartup>();
            });

   }
}
