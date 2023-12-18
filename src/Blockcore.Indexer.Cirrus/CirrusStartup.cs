using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blockcore.Indexer.Cirrus.Client;
using Blockcore.Indexer.Cirrus.Crypto;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Cirrus.Sync.SyncTasks;
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
using MongoDB.Bson.Serialization;

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

         services.Replace(new ServiceDescriptor(typeof(IScriptInterpreter), typeof(CirrusScriptToAddressParser), ServiceLifetime.Singleton));

         services.Replace(new ServiceDescriptor(typeof(IStorageOperations), typeof(CirrusMongoStorageOperations), ServiceLifetime.Singleton));

         services.Replace(new ServiceDescriptor(typeof(IStorage), typeof(CirrusMongoData), ServiceLifetime.Singleton));

         services.AddSingleton<ICirrusStorage, CirrusMongoData>();
         services.AddSingleton<ICirrusMongoDb, CirrusMongoDb>();

         services.AddControllers()
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddControllersAsServices();

         services.AddTransient<IComputeSmartContractService<DaoContractTable>,ComputeSmartContractServiceWithSplitDocuments<DaoContractTable,DaoContractProposalTable>>();
         services.AddTransient<IComputeSmartContractService<StandardTokenContractTable>,ComputeSmartContractServiceWithSplitDocuments<StandardTokenContractTable,StandardTokenHolderTable>>();
         services.AddTransient<IComputeSmartContractService<NonFungibleTokenContractTable>,ComputeSmartContractServiceWithSplitDocuments<NonFungibleTokenContractTable,NonFungibleTokenTable>>();

         services.AddTransient<ISmartContractTransactionsLookup<NonFungibleTokenContractTable>,NonFungibleTokenSmartContractTransactionsLookup>();
         services.AddTransient(typeof(ISmartContractTransactionsLookup<>), typeof(SmartContractTransactionsLookup<>));

         ScanAssemblyAndRegisterTypeByNameAsTransient(services, typeof(ILogReader<DaoContractTable,DaoContractProposalTable>),
            typeof(ILogReader<DaoContractTable,DaoContractProposalTable>).Assembly);
         ScanAssemblyAndRegisterTypeByNameAsTransient(services, typeof(ILogReader<StandardTokenContractTable,StandardTokenHolderTable>),
            typeof(ILogReader<StandardTokenContractTable,StandardTokenHolderTable>).Assembly);
         ScanAssemblyAndRegisterTypeByNameAsTransient(services, typeof(ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>),
            typeof(ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>).Assembly);

         services.AddTransient<ISmartContractHandlersFactory<DaoContractTable,DaoContractProposalTable>,SmartContractHandlersFactory<DaoContractTable,DaoContractProposalTable>>();
         services.AddTransient<ISmartContractHandlersFactory<StandardTokenContractTable,StandardTokenHolderTable>,SmartContractHandlersFactory<StandardTokenContractTable,StandardTokenHolderTable>>();
         services.AddTransient<ISmartContractHandlersFactory<NonFungibleTokenContractTable,NonFungibleTokenTable>,SmartContractHandlersFactory<NonFungibleTokenContractTable,NonFungibleTokenTable>>();

         RegisterSmartContractBuilder(services); //No need to scan the assembly as there won't be that many

         services.AddScoped<TaskRunner,SmartContractSyncRunner>();

         services.AddTransient<IBlockRewindOperation, CirrusBlockRewindOperation>();


         BsonSerializer.RegisterSerializer(typeof(IDictionary<string, object>), new ComplexTypeSerializer());
      }

      private static IServiceCollection RegisterSmartContractBuilder(IServiceCollection collection)
      {
         collection.AddTransient<ISmartContractBuilder<DaoContractTable>, DaoSmartContractBuilder>();
         collection.AddTransient<ISmartContractBuilder<StandardTokenContractTable>, StandardTokenSmartContractBuilder>();
         collection.AddTransient<ISmartContractBuilder<NonFungibleTokenContractTable>, NonFungibleTokenSmartContractBuilder>();
         return collection;
      }

      private static void ScanAssemblyAndRegisterTypeByNameAsTransient(IServiceCollection services, Type typeToRegister, Assembly assembly)
      {
         // Discovers and registers all type implementation in this assembly.
         var implementations = from type in assembly.GetTypes()
            where type.GetInterface(typeToRegister.Name) != null &&
                  type.GetInterface(typeToRegister.Name)
                     .GetGenericArguments()
                     .SequenceEqual(typeToRegister.GetGenericArguments())
            select new { Interface = typeToRegister, ImplementationType = type };

         foreach (var implementation in implementations)
         {
            services.AddTransient(implementation.Interface, implementation.ImplementationType);
         }
      }

      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         Startup.Configure(app, env);
      }
   }
}
