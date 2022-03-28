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

         SetDocumentMapAndIgnoreExtraElements<CirrusContractTable>();
         SetDocumentMapAndIgnoreExtraElements<CirrusContractCodeTable>();
         SetDocumentMapAndIgnoreExtraElements<DaoContractComputedTable>();
         SetDocumentMapAndIgnoreExtraElements<StandardTokenComputedTable>();
         SetDocumentMapAndIgnoreExtraElements<NonFungibleTokenComputedTable>();


         return Task.FromResult(1);
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
