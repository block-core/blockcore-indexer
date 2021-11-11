using System.Threading.Tasks;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Sync.SyncTasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Storage.Mongo
{
   public class MongoBuilderPocAddress : TaskStarter
   {
      private readonly MongoData mongoData;

      private readonly ILogger<MongoBuilderPocAddress> log;

      private readonly IndexerSettings configuration;

      /// <summary>
      /// Initializes a new instance of the <see cref="MongoBuilder"/> class.
      /// </summary>
      public MongoBuilderPocAddress(ILogger<MongoBuilderPocAddress> logger, IStorage data, IOptions<IndexerSettings> nakoConfiguration)
          : base(logger)
      {
         log = logger;
         mongoData = (MongoData)data;
         configuration = nakoConfiguration.Value;
      }

      public override int Priority
      {
         get
         {
            return 20;
         }
      }

      public override Task OnExecute()
      {
         log.LogTrace("MongoBuilder: Creating mappings");

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressForOutput)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressForOutput>(cm =>
                {
                   cm.AutoMap();
                   //cm.MapIdMember(c => c.Outpoint);
                });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressForInput)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressForInput>(cm =>
            {
               cm.AutoMap();
               //cm.MapIdMember(c => c.Outpoint);
            });
         }



         // indexes
         log.LogTrace("MongoBuilder: Creating indexes");

         //IndexKeysDefinition<AddressTransaction> addressIndex = Builders<AddressTransaction>.IndexKeys.Ascending(blk => blk.Address);
         //mongoData.AddressTransaction.Indexes.CreateOne(addressIndex);

         //IndexKeysDefinition<AddressForOutput> transactionIndex = Builders<AddressForOutput>.IndexKeys.Ascending(blk => blk.TransactionId);
         //mongoData.AddressTransaction.Indexes.CreateOne(transactionIndex);

         return Task.FromResult(1);
      }
   }
}
