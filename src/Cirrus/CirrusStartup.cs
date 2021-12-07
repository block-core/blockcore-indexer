using Blockcore.Indexer;
using Blockcore.Indexer.Storage;
using Cirrus.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
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
      }


      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         app.UseExceptionHandler("/error");

         // Enable Cors
         app.UseCors("IndexerPolicy");

         app.UseResponseCompression();

         //app.UseMvc();

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

      /// <summary>
      /// Hide Stratis related endpoints in Swagger shown due to using Nuget packages
      /// in WebApi project for serialization.
      /// </summary>
      public class ActionHidingConvention : IActionModelConvention
      {
         public void Apply(ActionModel action)
         {
            // Replace with any logic you want
            if (!action.Controller.DisplayName.Contains("Blockcore.Indexer"))
            {
               action.ApiExplorer.IsVisible = false;
            }
         }
      }
   }
}
