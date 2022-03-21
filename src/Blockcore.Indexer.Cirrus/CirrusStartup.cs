using System;
using System.Linq;
using System.Reflection;
using Blockcore.Indexer.Cirrus.Client;
using Blockcore.Indexer.Cirrus.Crypto;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blockcore.Indexer.Cirrus
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

         ServiceDescriptor descriptor = services.First(_ => _.ImplementationType == typeof(MongoBuilder));
         services.Remove(descriptor);
         services.AddSingleton<TaskStarter, CirrusMongoBuilder>();

         services.Replace(new ServiceDescriptor(typeof(ISyncBlockTransactionOperationBuilder), typeof(CirrusSyncBlockTransactionOperationBuilder),
            ServiceLifetime.Singleton));

         services.Replace(new ServiceDescriptor(typeof(IScriptInterpeter), typeof(CirrusScriptToAddressParser), ServiceLifetime.Singleton));

         services.Replace(new ServiceDescriptor(typeof(IStorageOperations), typeof(CirrusMongoStorageOperations), ServiceLifetime.Singleton));

         services.Replace(new ServiceDescriptor(typeof(IStorage), typeof(CirrusMongoData), ServiceLifetime.Singleton));

         services.AddSingleton<ICirrusStorage, CirrusMongoData>();
         services.AddSingleton<ICirrusMongoDb, CirrusMongoDb>();

         services.AddControllers()
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddControllersAsServices();

         services.AddTransient(typeof(IComputeSmartContractService<>),typeof(ComputeSmartContractService<>));
         services.AddTransient(typeof(ILogReaderFactory<>),typeof(LogReaderFactory<>));

         ScanAssemblyAndRegisterTypeByNameAsTransient(services, typeof(ILogReader<DaoContractComputedTable>),
            typeof(ILogReader<>).Assembly);

         ScanAssemblyAndRegisterTypeByNameAsTransient(services, typeof(ILogReader<StandardTokenComputedTable>),
            typeof(ILogReader<>).Assembly);
      }

      // private static IServiceCollection RegisterLogReaders(IServiceCollection collection)
      // {
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, WhitelistAddressesLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, BlacklistAddressesLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, CreateProposalLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, DepositLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, ExecuteProposalLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, GetOrPropertyCallsLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, UpdateMaxVotingDurationLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, UpdateMinVotingDurationLogReader>();
      //    collection.AddTransient<ILogReader<DaoContractComputedTable>, VoteLogReader>();
      //    return collection;
      // }

      private static void ScanAssemblyAndRegisterTypeByNameAsTransient(IServiceCollection services, Type typeToRegister, Assembly assembly)
      {
         // Discovers and registers all type implementation in this assembly.
         var implementations = from type in assembly.GetTypes()
            where type.GetInterface(typeToRegister.Name) != null &&
                  type.GetGenericArguments().Equals(typeToRegister.GetGenericArguments())
            select new { Interface = typeToRegister, ImplementationType = type };

         foreach (var implementation in implementations)
         {
            services.AddTransient(implementation.Interface, implementation.ImplementationType);
         }
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
