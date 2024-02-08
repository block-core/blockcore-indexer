using Blockcore.Indexer.Angor;
using Blockcore.Indexer.Core.Extensions;

var builder = Host.CreateDefaultBuilder(args)
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

      webBuilder.UseStartup<AngorStartup>();
   });

builder.Build().Run();
