using System.Linq;
using Blockcore.Indexer;
using Blockcore.Indexer.Client;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Sync.SyncTasks;
using Cirrus.Client;
using Cirrus.Storage;
using Cirrus.Storage.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cirrus
{
   public class CirrusStartup
   {
      public IConfiguration Configuration { get; }

      public CirrusStartup(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      public void ConfigureServices(IServiceCollection services)
      {
         Startup.AddIndexerServices(services,Configuration);

         services.Replace(new ServiceDescriptor(typeof(IMapMongoBlockToStorageBlock), typeof(CirrusBlockMapping),
            ServiceLifetime.Transient));

         services.Replace(new ServiceDescriptor(typeof(ICryptoClientFactory), typeof(CirrusClientFactory),
            ServiceLifetime.Singleton));

         var dercriptor = services.First(_ => _.ImplementationType == typeof(MongoBuilder));
         services.Remove(dercriptor);
         services.AddSingleton<TaskStarter, CirrusMongoBuilder>();


         services.AddMvc()
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddControllersAsServices();
      }


      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         app.UseExceptionHandler("/error");

         // Enable Cors
         app.UseCors("IndexerPolicy");

         app.UseResponseCompression();

         app.UseDefaultFiles();

         app.UseStaticFiles();

         app.UseRouting();

         app.UseSwagger(c =>
         {
            c.RouteTemplate = "docs/{documentName}/openapi.json";
         });

         app.UseSwaggerUI(c =>
         {
            c.RoutePrefix = "docs";
            c.SwaggerEndpoint("/docs/indexer/openapi.json", "Blockcore Indexer API");
         });

         app.UseEndpoints(endpoints =>
         {
            endpoints.MapControllers();
         });
      }
   }
}
