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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressTransaction)))
         {
            // MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressTransaction>(cm =>
            //     {
            //        cm.AutoMap();
            //        cm.MapIdMember(c => c.UniquId);
            //     });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressForInput)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressForInput>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.UniquID);
            });
         }



         // indexes
         log.LogTrace("MongoBuilder: Creating indexes");

         IndexKeysDefinition<AddressTransaction> blkIndex = Builders<AddressTransaction>.IndexKeys.Ascending(blk => blk.Address);
         mongoData.AddressTransaction.Indexes.CreateOne(blkIndex);

         return Task.FromResult(1);
      }
   }
}
