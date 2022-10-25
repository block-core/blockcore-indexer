using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoBuilder : MongoBuilder
   {
      ICirrusMongoDb cirrusMongoDb;

      public CirrusMongoBuilder(ILogger<MongoBuilder> logger, ICirrusMongoDb data, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainSettings)
         : base(logger, data, nakoConfiguration,chainSettings)
      {
         cirrusMongoDb = data;
      }

      public override Task OnExecute()
      {
         base.OnExecute();

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(CirrusBlock)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<CirrusBlock>();
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(SmartContractTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<SmartContractTable>(_ =>
            {
               _.AutoMap();
               _.MapIdProperty(_ => _.ContractAddress);
               _.SetIsRootClass(true);
               _.SetDiscriminator(nameof(SmartContractTable));
            });
         }

         SetDocumentMapAndIgnoreExtraElements<DaoContractTable>();
         SetDocumentMapAndIgnoreExtraElements<StandardTokenContractTable>();
         SetDocumentMapAndIgnoreExtraElements<NonFungibleTokenContractTable>();

         SetDocumentMapAndIgnoreExtraElements<CirrusContractTable>();
         SetDocumentMapAndIgnoreExtraElements<CirrusContractCodeTable>();
         SetDocumentMapAndIgnoreExtraElements<DaoContractTable>();
         SetDocumentMapAndIgnoreExtraElements<StandardTokenHolderTable>();
         SetDocumentMapAndIgnoreExtraElements<DaoContractProposalTable>();

         cirrusMongoDb.CirrusContractTable.Indexes
            .CreateOne(new CreateIndexModel<CirrusContractTable>(Builders<CirrusContractTable>
               .IndexKeys.Ascending(_ => _.BlockIndex))); //TODO move this to the block indexer task runner, but we'll need to move the indexes in there to a different class for each project/blockchain

         return Task.CompletedTask;
      }

      static void SetDocumentMapAndIgnoreExtraElements<T>()
      {
         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(T)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<T>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }
      }
   }
}
