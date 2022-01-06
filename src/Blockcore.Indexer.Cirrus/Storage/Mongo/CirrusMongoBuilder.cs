using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoBuilder : MongoBuilder
   {
      public CirrusMongoBuilder(ILogger<MongoBuilder> logger, IStorage data, IOptions<IndexerSettings> nakoConfiguration)
         : base(logger, data, nakoConfiguration)
      {
      }

      public override Task OnExecute()
      {
         base.OnExecute();

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(CirrusBlock)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<CirrusBlock>();
         }

         if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(CirrusContractTable)))
         {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<CirrusContractTable>(cm =>
            {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
            });
         }


         return Task.FromResult(1);
      }
   }
}
