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
      private readonly IMongoDb mongoDb;

      private readonly ILogger<MongoBuilder> log;
      readonly ChainSettings chainConfiguration;
      private readonly IndexerSettings configuration;

      /// <summary>
      /// Initializes a new instance of the <see cref="MongoBuilder"/> class.
      /// </summary>
      public MongoBuilder(ILogger<MongoBuilder> logger, IMongoDb data, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainSettings)
          : base(logger)
      {
         log = logger;
         mongoDb = data;
         configuration = nakoConfiguration.Value;
         chainConfiguration = chainSettings.Value;
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

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(ReorgBlockTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<ReorgBlockTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }

         mongoDb.UnspentOutputTable.Indexes
            .CreateOne(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
               .IndexKeys.Hashed(trxBlk => trxBlk.Outpoint)));
         // To avoid the duplicate trx hash error on btc and save on perf dont create this index on the Bitcoin network.
         if (chainConfiguration.Symbol.ToUpper() != "BTC")
         {
            // TODO: this index was added to ensure the table is not getting corrupted with duplicated values
            // however this is not expected and once we sure the code is stable we can remove this index to gain
            // better performance on initial sync, onother options is to add this index at the end of the initial
            // sync where natural reorgs are expected pretty often
            mongoDb.UnspentOutputTable.Indexes
               .CreateOne(new CreateIndexModel<UnspentOutputTable>(Builders<UnspentOutputTable>
                  .IndexKeys.Ascending(trxBlk => trxBlk.Outpoint), new CreateIndexOptions { Unique = true }));
         }

         mongoDb.ReorgBlock.Indexes
            .CreateOne(new CreateIndexModel<ReorgBlockTable>(Builders<ReorgBlockTable>
               .IndexKeys.Descending(_ => _.BlockIndex)));

         return Task.FromResult(1);
      }
   }
}
