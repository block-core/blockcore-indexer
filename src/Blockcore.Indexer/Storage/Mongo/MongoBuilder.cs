using Cloo;

namespace Blockcore.Indexer.Storage.Mongo
{
   using System.Threading.Tasks;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Storage.Mongo.Types;
   using Blockcore.Indexer.Sync.SyncTasks;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;
   using MongoDB.Driver;

   /// <summary>
   /// The mongo builder.
   /// </summary>
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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapBlock)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapBlock>(cm =>
                {
                   cm.AutoMap();
                   cm.MapIdMember(c => c.BlockHash);
                });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapTransactionAddress)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapTransactionAddress>(cm =>
                {
                   cm.AutoMap();
                   cm.MapIdMember(c => c.Id);
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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(AddressHistoryComputed)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<AddressHistoryComputed>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.Id);
            });
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapTransactionBlock)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapTransactionBlock>(cm =>
                {
                   cm.AutoMap();
                   cm.MapIdMember(c => c.TransactionId);
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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(MapRichlist)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<MapRichlist>(cm =>
            {
               cm.AutoMap();
               cm.MapIdMember(c => c.Address);
            });
         }

         // indexes
         log.LogTrace("MongoBuilder: Creating indexes");

         IndexKeysDefinition<MapBlock> blkIndex = Builders<MapBlock>.IndexKeys.Ascending(blk => blk.BlockIndex);
         mongoData.MapBlock.Indexes.CreateOne(blkIndex);

         IndexKeysDefinition<MapTransactionAddress> addrIndex = Builders<MapTransactionAddress>.IndexKeys.Ascending(addr => addr.Addresses);
         mongoData.MapTransactionAddress.Indexes.CreateOne(addrIndex);

         IndexKeysDefinition<MapTransactionAddress> addrBlockIndex = Builders<MapTransactionAddress>.IndexKeys.Ascending(addr => addr.BlockIndex);
         mongoData.MapTransactionAddress.Indexes.CreateOne(addrBlockIndex);

         IndexKeysDefinition<MapTransactionAddress> addrSpentBlockIndex = Builders<MapTransactionAddress>.IndexKeys.Ascending(addr => addr.SpendingBlockIndex);
         mongoData.MapTransactionAddress.Indexes.CreateOne(addrSpentBlockIndex);

         IndexKeysDefinition<MapTransactionBlock> trxBlkIndex = Builders<MapTransactionBlock>.IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex);
         mongoData.MapTransactionBlock.Indexes.CreateOne(trxBlkIndex);

         IndexKeysDefinition<MapRichlist> richListIndex = Builders<MapRichlist>.IndexKeys.Ascending(i => i.Balance);
         mongoData.MapRichlist.Indexes.CreateOne(richListIndex);

         IndexKeysDefinition<AddressComputed> addrComp = Builders<AddressComputed>.IndexKeys.Ascending(i => i.Address);
         mongoData.AddressComputed.Indexes.CreateOne(addrComp);

         //IndexKeysDefinition<MapTransactionAddressHistoryComputed> addrHistory = Builders<MapTransactionAddressHistoryComputed>.IndexKeys.Ascending(i => i.BlockIndex);
         //mongoData.MapTransactionAddressHistoryComputed.Indexes.CreateOne(addrHistory);

         IndexKeysDefinition<AddressHistoryComputed> addrHistory1 = Builders<AddressHistoryComputed>.IndexKeys.Descending(i => i.BlockIndex);
         mongoData.AddressHistoryComputed.Indexes.CreateOne(addrHistory1);

         IndexKeysDefinition<AddressHistoryComputed> addrHistory2 = Builders<AddressHistoryComputed>.IndexKeys.Descending(i => i.Position);
         mongoData.AddressHistoryComputed.Indexes.CreateOne(addrHistory2);

         return Task.FromResult(1);
      }
   }
}
