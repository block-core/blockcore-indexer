using System.Threading.Tasks;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class MongoBuilder : TaskStarter
   {
      private readonly MongoData mongoData;

      private readonly ILogger<MongoBuilder> log;

      private readonly IndexerSettings configuration;

      /// <summary>
      /// Initializes a new instance of the <see cref="MongoBuilder"/> class.
      /// </summary>
      public MongoBuilder(ILogger<MongoBuilder> logger, IStorage data, IOptions<IndexerSettings> nakoConfiguration)
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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(BlockTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<BlockTable>(cm =>
            {
               cm.AutoMap();
               //cm.MapIdMember(c => c.BlockHash);
               cm.MapIdMember(c => c.BlockIndex);
               cm.SetIsRootClass(true);
               cm.SetDiscriminator(nameof(BlockTable));
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TransactionBlockTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<TransactionBlockTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
               //cm.MapIdMember(c => c.TransactionId);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TransactionTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<TransactionTable>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.TransactionId);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(OutputTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<OutputTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(InputTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<InputTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressComputedTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressComputedTable>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.Id);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressHistoryComputedTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressHistoryComputedTable>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.Id);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressUtxoComputedTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressUtxoComputedTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MempoolTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MempoolTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(UnspentOutputTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<UnspentOutputTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         mongoData.UnspentOutputTable.Indexes
            .CreateOne(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
               .IndexKeys.Hashed(trxBlk => trxBlk.Outpoint)));

         return Task.FromResult(1);
      }
   }
}
