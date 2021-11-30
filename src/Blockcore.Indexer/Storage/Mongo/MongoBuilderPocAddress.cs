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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapBlock)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapBlock>(cm =>
            {
               cm.AutoMap();
               //cm.MapIdMember(c => c.BlockHash);
               cm.MapIdMember(c => c.BlockIndex);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapTransactionBlock)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapTransactionBlock>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
               //cm.MapIdMember(c => c.TransactionId);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapTransaction)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapTransaction>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.TransactionId);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressForOutput)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressForOutput>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressForInput)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressForInput>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressComputed)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressComputed>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.Id);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(                                                             AddressHistoryComputed)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressHistoryComputed>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.Id);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Mempool)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Mempool>(cm =>
            {
               cm.AutoMap();
            });
         }


         // indexes
         log.LogTrace("MongoBuilder: Creating indexes");

         //IndexKeysDefinition<MapBlock> blkIndex = Builders<MapBlock>.IndexKeys.Ascending(blk => blk.BlockIndex);
         //mongoData.MapBlock.Indexes.CreateOne(blkIndex);

         //IndexKeysDefinition<MapTransactionBlock> trxBlkIndex = Builders<MapTransactionBlock>.IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex);
         //mongoData.MapTransactionBlock.Indexes.CreateOne(trxBlkIndex);

         //IndexKeysDefinition<AddressTransaction> addressIndex = Builders<AddressTransaction>.IndexKeys.Ascending(blk => blk.Address);
         //mongoData.AddressTransaction.Indexes.CreateOne(addressIndex);

         // mongoData.AddressForOutput.Indexes.CreateOne(
         //    Builders<AddressForOutput>.IndexKeys.Ascending(blk => blk.hashedOutpoint),
         //    new CreateIndexOptions { Unique = true });

         return Task.FromResult(1);
      }
   }
}
