using System;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.SyncTasks;
using Blockcore.Indexer.Core.Storage.Postgres;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage;

public static class DatabaseRegistration
{
   public static IServiceCollection AddMongoDatabase(this IServiceCollection services)
   {
      services.AddSingleton<TaskStarter, MongoBuilder>();
      services.AddSingleton<IMongoDb, MongoDb>();
      services.AddSingleton<IStorage, MongoData>();
      services.AddSingleton<IStorageOperations, MongoStorageOperations>();
      services.AddTransient<IMapMongoBlockToStorageBlock, MapMongoBlockToStorageBlock>();
      services.AddScoped<TaskRunner, MongoDbBlockIndexer>();
      services.AddSingleton<IStorageBatchFactory, MongoStorageBatchFactory>();
      services.AddTransient<IMondoDbInfo, MondoDbInfo>();
      //TODO add this for address driven blockchains
      //services.AddScoped<TaskRunner, RichListScanning>();

      services.AddSingleton(_ =>
      {
         var indexerConfiguration = _.GetService(typeof(IOptions<IndexerSettings>)) as IOptions<IndexerSettings>;// configuration.GetSection("Indexer") as IndexerSettings;
         var chainConfiguration = _.GetService(typeof(IOptions<ChainSettings>)) as IOptions<ChainSettings>;//  configuration.GetSection("Chain") as ChainSettings;

         var mongoClient = new MongoClient(indexerConfiguration.Value.ConnectionString.Replace("{Symbol}",
            chainConfiguration.Value.Symbol.ToLower()));

         string dbName = indexerConfiguration.Value.DatabaseNameSubfix
            ? $"Blockchain{chainConfiguration.Value.Symbol}"
            : "Blockchain";

         return mongoClient.GetDatabase(dbName);
      });

      // TODO: Verify that it is OK we add this to shared Startup for Blockcore and Cirrus.
      services.AddTransient<Mongo.IBlockRewindOperation, Mongo.BlockRewindOperation>();

      return services;
   }

   public static IServiceCollection AddPostgresDatabase(this IServiceCollection services)
   {
      services.AddSingleton<IStorage, PostgresData>();
      services.AddSingleton<IStorageOperations, PostgresStorageOperations>();
      services.AddSingleton<IMapPgBlockToStorageBlock, MapPgBlockToStorageBlock>();
      services.AddSingleton<IStorageBatchFactory, PostgresStorageBatchFactory>();
      services.AddSingleton<Postgres.IBlockRewindOperation, Postgres.BlockRewindOperation>();
      services.AddDbContextFactory<PostgresDbContext>((serviceProvider, options) =>
      {
         var indexerConfiguration = serviceProvider.GetService(typeof(IOptions<IndexerSettings>)) as IOptions<IndexerSettings>;
         var chainConfiguration = serviceProvider.GetService(typeof(IOptions<ChainSettings>)) as IOptions<ChainSettings>;

         string dbName = indexerConfiguration.Value.DatabaseNameSubfix
                     ? $"Blockchain{chainConfiguration.Value.Symbol}"
                     : "Blockchain";
         Console.WriteLine(indexerConfiguration.Value.ConnectionString);
         string connectionString = indexerConfiguration.Value.ConnectionString.Replace("{Symbol}", dbName);
         Console.WriteLine(dbName);
         Console.WriteLine("Connection string => " + connectionString);

         options.UseNpgsql(connectionString);
      }/*, ServiceLifetime.Singleton*/);


      return services;
   }
}
