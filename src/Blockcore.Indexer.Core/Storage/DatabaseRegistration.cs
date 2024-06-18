using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.SyncTasks;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage;

public static class DatabaseRegistration
{
   public static IServiceCollection AddMongoDatabase( this IServiceCollection services)
   {
      services.AddSingleton<TaskStarter, MongoBuilder>();
      services.AddSingleton<IMongoDb, MongoDb>();
      services.AddSingleton<IStorage, MongoData>();
      services.AddSingleton<IStorageOperations, MongoStorageOperations>();
      services.AddTransient<IMapMongoBlockToStorageBlock, MapMongoBlockToStorageBlock>();
      services.AddScoped<TaskRunner, MongoDbBlockIndexer>();
      services.AddSingleton<IStorageBatchFactory, MongoStorageBatchFactory>();
      //TODO add this for address driven blockchains
      //services.AddScoped<TaskRunner, RichListScanning>();

      services.AddSingleton(_ =>
      {
         var indexerConfiguration = _.GetService(typeof(IOptions<IndexerSettings>))as IOptions<IndexerSettings> ;// configuration.GetSection("Indexer") as IndexerSettings;
         var chainConfiguration  = _.GetService(typeof(IOptions<ChainSettings>)) as IOptions<ChainSettings>;//  configuration.GetSection("Chain") as ChainSettings;

         var mongoClient = new MongoClient(indexerConfiguration.Value.ConnectionString.Replace("{Symbol}",
            chainConfiguration.Value.Symbol.ToLower()));

         string dbName = indexerConfiguration.Value.DatabaseNameSubfix
            ? $"Blockchain{chainConfiguration.Value.Symbol}"
            : "Blockchain";

         return mongoClient.GetDatabase(dbName);
      });

      return services;
   }
}
