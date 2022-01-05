using System.Threading.Tasks;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
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

         return Task.FromResult(1);
      }
   }
}
