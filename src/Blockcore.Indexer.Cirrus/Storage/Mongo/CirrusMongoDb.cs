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

   public IMongoCollection<DaoContractTable> DaoContractComputedTable
   {
      get
      {
         return mongoDatabase.GetCollection<DaoContractTable>("SmartContractTable");
      }
   }

   public IMongoCollection<StandardTokenContractTable> StandardTokenComputedTable
   {
      get
      {
         return mongoDatabase.GetCollection<StandardTokenContractTable>("SmartContractTable");
      }
   }

   public IMongoCollection<NonFungibleTokenContractTable> NonFungibleTokenComputedTable
   {
      get
      {
         return mongoDatabase.GetCollection<NonFungibleTokenContractTable>("SmartContractTable");
      }
   }

   public IMongoCollection<NonFungibleTokenTable> NonFungibleTokenTable
   {
      get
      {
         return mongoDatabase.GetCollection<NonFungibleTokenTable>("NonFungibleToken");
      }
   }

   public IMongoCollection<DaoContractProposalTable> DaoContractProposalTable
   {
      get
      {
         return mongoDatabase.GetCollection<DaoContractProposalTable>("DaoContractProposal");
      }
   }

   public IMongoCollection<StandardTokenHolderTable> StandardTokenHolderTable
   {
      get
      {
         return mongoDatabase.GetCollection<StandardTokenHolderTable>("StandardTokenHolder");
      }
   }
}
