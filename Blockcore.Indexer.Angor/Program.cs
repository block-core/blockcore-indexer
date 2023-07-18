using Blockcore.Indexer;
using Blockcore.Indexer.Core.Extensions;

var builder = Host.CreateDefaultBuilder()
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

      webBuilder.UseStartup<BlockcoreStartup>();
   });

builder.Build().Run();

// Startup.AddIndexerServices(builder.Services, builder.Configuration);
// builder.Services.Configure<HostOptions>(option =>
// {
//    // the BlockStore task can take long time to complete
//    // to avoid rewind on shutdown we allow it extra time
//    option.ShutdownTimeout = System.TimeSpan.FromSeconds(20);
// });
//
//
// builder.Configuration.AddBlockcore("Blockore Indexer", args);
//
// //builder.Configuration.AddBlockcore("Blockore Indexer", args);
//
// builder.WebHost.ConfigureKestrel(serverOptions =>
// {
//    serverOptions.AddServerHeader = false;
// });
//
// var app = builder.Build();
//
// Startup.Configure(app, null);
//
//
// app.Run();
