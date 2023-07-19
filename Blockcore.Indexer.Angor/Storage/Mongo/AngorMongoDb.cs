using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using MongoDB.Driver;

namespace Blockcore.Indexer.Angor.Storage.Mongo;

public class AngorMongoDb : MongoDb,IAngorMongoDb
{
   public AngorMongoDb(ILogger<AngorMongoDb> logger, IMongoDatabase mongoDatabase) : base(logger, mongoDatabase)
   { }

   public IMongoCollection<Project> ProjectTable
   {
      get
      {
         return mongoDatabase.GetCollection<Project>("Project");
      }
   }

   public IMongoCollection<Investment> InvestmentTable
   {
      get
      {
         return mongoDatabase.GetCollection<Investment>("Investment");
      }
   }
}
