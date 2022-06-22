using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoBuilder : MongoBuilder
   {
      public CirrusMongoBuilder(ILogger<MongoBuilder> logger, IMongoDb data, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainSettings)
         : base(logger, data, nakoConfiguration,chainSettings)
      {
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

         SetDocumentMapAndIgnoreExtraElements<CirrusContractTable>();
         SetDocumentMapAndIgnoreExtraElements<CirrusContractCodeTable>();
         SetDocumentMapAndIgnoreExtraElements<DaoContractTable>();
         // SetDocumentMapAndIgnoreExtraElements<StandardTokenContractTable>();
         // SetDocumentMapAndIgnoreExtraElements<NonFungibleTokenContractTable>();
         // SetDocumentMapAndIgnoreExtraElements<NonFungibleTokenTable>();
         SetDocumentMapAndIgnoreExtraElements<StandardTokenHolderTable>();
         SetDocumentMapAndIgnoreExtraElements<DaoContractProposalTable>();

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
