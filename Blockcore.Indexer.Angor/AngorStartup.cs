using Blockcore.Indexer.Angor.Storage;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Core;

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
      services.AddSingleton<IAngorStorage, AngorMongoData>();
      services.AddSingleton<IAngorMongoDb, AngorMongoDb>();

   }

   public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
      Startup.Configure(app, env);
   }
}
