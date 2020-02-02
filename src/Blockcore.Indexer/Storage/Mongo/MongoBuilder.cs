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

         // indexes
         log.LogTrace("MongoBuilder: Creating indexes");

         IndexKeysDefinition<MapBlock> blkIndex = Builders<MapBlock>.IndexKeys.Ascending(blk => blk.BlockIndex);
         mongoData.MapBlock.Indexes.CreateOne(blkIndex);

         IndexKeysDefinition<MapTransactionAddress> addrIndex = Builders<MapTransactionAddress>.IndexKeys.Ascending(addr => addr.Addresses);
         mongoData.MapTransactionAddress.Indexes.CreateOne(addrIndex);
         IndexKeysDefinition<MapTransactionAddress> addrBlockIndex = Builders<MapTransactionAddress>.IndexKeys.Ascending(addr => addr.BlockIndex);
         mongoData.MapTransactionAddress.Indexes.CreateOne(addrBlockIndex);

         IndexKeysDefinition<MapTransactionBlock> trxBlkIndex = Builders<MapTransactionBlock>.IndexKeys.Ascending(trxBlk => trxBlk.BlockIndex);
         mongoData.MapTransactionBlock.Indexes.CreateOne(trxBlkIndex);

         // This is not needed as the id field is already the index
         //var trxIndex = Builders<MapTransaction>.IndexKeys.Ascending(trxBlk => trxBlk.TransactionId);
         //this.mongoData.MapTransaction.Indexes.CreateOne(trxIndex);

         return Task.FromResult(1);
      }
   }
}
