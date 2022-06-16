using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public class CirrusMongoDb : MongoDb, ICirrusMongoDb
{
   public CirrusMongoDb(ILogger<MongoDb> logger, IMongoDatabase mongoDatabase) : base(logger, mongoDatabase)
   { }

   public IMongoCollection<CirrusContractTable> CirrusContractTable
   {
      get
      {
         return mongoDatabase.GetCollection<CirrusContractTable>("CirrusContract");
      }
   }

   public IMongoCollection<CirrusContractCodeTable> CirrusContractCodeTable
   {
      get
      {
         return mongoDatabase.GetCollection<CirrusContractCodeTable>("CirrusContractCode");
      }
   }

   public IMongoCollection<DaoContractComputedTable> DaoContractComputedTable
   {
      get
      {
         return mongoDatabase.GetCollection<DaoContractComputedTable>("DaoContractComputed");
      }
   }

   public IMongoCollection<StandardTokenComputedTable> StandardTokenComputedTable
   {
      get
      {
         return mongoDatabase.GetCollection<StandardTokenComputedTable>("StandardTokenComputed");
      }
   }

   public IMongoCollection<NonFungibleTokenComputedTable> NonFungibleTokenComputedTable
   {
      get
      {
         return mongoDatabase.GetCollection<NonFungibleTokenComputedTable>("NonFungibleTokenComputed");
      }
   }

   public IMongoCollection<NonFungibleToken> NonFungibleTokenTable
   {
      get
      {
         return mongoDatabase.GetCollection<NonFungibleToken>("NonFungibleToken");
      }
   }
}
