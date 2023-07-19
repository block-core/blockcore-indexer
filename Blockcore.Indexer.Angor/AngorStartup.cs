using Blockcore.Indexer.Angor.Storage;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Core;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blockcore.Indexer.Angor;

public class AngorStartup
{
   public IConfiguration Configuration { get; }

   public AngorStartup(IConfiguration configuration)
   {
      Configuration = configuration;
   }

   public void ConfigureServices(IServiceCollection services)
   {
      Startup.AddIndexerServices(services,Configuration);

      ServiceDescriptor descriptor = services.First(_ => _.ImplementationType == typeof(MongoBuilder));
      services.Remove(descriptor);
      services.AddSingleton<TaskStarter, AngorMongoBuilder>();

      services.Replace(new ServiceDescriptor(typeof(IStorage), typeof(AngorMongoData), ServiceLifetime.Scoped));

      services.AddSingleton<IAngorStorage, AngorMongoData>();
      services.AddSingleton<IAngorMongoDb, AngorMongoDb>();

   }

   public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
      Startup.Configure(app, env);
   }
}
