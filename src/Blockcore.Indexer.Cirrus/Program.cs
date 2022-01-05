using Blockcore;
using Blockcore.Indexer.Core.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Indexer.Cirrus
{
   using System.Linq;
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
               // If there are no specific chain, e.g. "main" used when local, we'll force the CRS chain.
               if (!string.Join(',', args).Contains("--chain"))
               {
                  args = args.Concat(new string[] { "--chain=CRS" }).ToArray();
               }

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
