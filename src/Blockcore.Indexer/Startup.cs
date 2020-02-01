using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Blockcore.Indexer.Api.Handlers;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Operations;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Sync;
using Blockcore.Indexer.Sync.SyncTasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.OpenApi.Models;

namespace Blockcore.Indexer
{
   public class Startup
   {
      public IConfiguration Configuration { get; }

      public Startup(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      public void ConfigureServices(IServiceCollection services)
      {
         services.Configure<ChainSettings>(Configuration.GetSection("Chain"));
         services.Configure<NetworkSettings>(Configuration.GetSection("Network"));
         services.Configure<IndexerSettings>(Configuration.GetSection("Indexer"));

         services.AddSingleton<QueryHandler>();
         services.AddSingleton<StatsHandler>();
         services.AddSingleton<CommandHandler>();
         services.AddSingleton<IStorage, MongoData>();
         services.AddSingleton<IStorageOperations, MongoStorageOperations>();
         services.AddSingleton<TaskStarter, MongoBuilder>();
         services.AddTransient<SyncServer>();
         services.AddSingleton<SyncConnection>();
         services.AddSingleton<ISyncOperations, SyncOperations>();
         services.AddScoped<Runner>();
         services.AddScoped<TaskRunner, BlockFinder>();
         services.AddScoped<TaskRunner, BlockStore>();
         services.AddScoped<TaskRunner, BlockSyncer>();
         services.AddScoped<TaskRunner, PoolFinder>();
         services.AddScoped<TaskRunner, Notifier>();
         services.AddScoped<TaskStarter, BlockReorger>();

         services.AddMemoryCache();
         services.AddHostedService<SyncServer>();

         services.AddControllers().AddNewtonsoftJson(options =>
         {
            options.SerializerSettings.FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.DefaultValue;
         });

         services.AddSwaggerGen(
             options =>
             {
                // TODO: Decide which version to use.
                string assemblyVersion = typeof(Startup).Assembly.GetName().Version.ToString();
                string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

                options.SwaggerDoc("indexer", new OpenApiInfo { Title = "Blockcore Indexer API", Version = fileVersion });

                // integrate xml comments
                if (File.Exists(XmlCommentsFilePath))
                {
                   options.IncludeXmlComments(XmlCommentsFilePath);
                }

                options.DescribeAllEnumsAsStrings();

                options.DescribeStringEnumsInCamelCase();
             });

         services.AddSwaggerGenNewtonsoftSupport(); // explicit opt-in - needs to be placed after AddSwaggerGen()

         services.AddCors(o => o.AddPolicy("IndexerPolicy", builder =>
         {
            builder.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
         }));
      }

      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         app.UseExceptionHandler("/error");

         // Enable Cors
         app.UseCors("IndexerPolicy");

         //app.UseMvc();

         app.UseDefaultFiles();

         app.UseStaticFiles();

         app.UseRouting();

         app.UseEndpoints(endpoints =>
         {
            endpoints.MapControllers();
         });

         app.UseSwagger(c =>
         {
            c.RouteTemplate = "docs/{documentName}/openapi.json";
         });

         app.UseSwaggerUI(c =>
         {
            c.RoutePrefix = "docs";
            c.SwaggerEndpoint("/docs/indexer/openapi.json", "Blockcore Indexer API");
         });
      }

      static string XmlCommentsFilePath
      {
         get
         {
            string basePath = PlatformServices.Default.Application.ApplicationBasePath;
            string fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
            return Path.Combine(basePath, fileName);
         }
      }
   }
}
