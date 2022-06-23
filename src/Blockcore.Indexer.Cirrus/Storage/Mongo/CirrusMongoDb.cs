using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public class CirrusMongoDb : MongoDb, ICirrusMongoDb
{
   private const string SmartContractTableName = "SmartContract";

   public CirrusMongoDb(ILogger<MongoDb> logger, IMongoDatabase mongoDatabase) : base(logger, mongoDatabase)
   { }

   public IMongoCollection<SmartContractTable> SmartContractTable
   {
      get
      {
         return mongoDatabase.GetCollection<SmartContractTable>(SmartContractTableName);
      }
   }

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

   public IMongoCollection<DaoContractTable> DaoContractTable
   {
      get
      {
         return mongoDatabase.GetCollection<DaoContractTable>(SmartContractTableName);
      }
   }

   public IMongoCollection<StandardTokenContractTable> StandardTokenContractTable
   {
      get
      {
         return mongoDatabase.GetCollection<StandardTokenContractTable>(SmartContractTableName);
      }
   }

   public IMongoCollection<NonFungibleTokenContractTable> NonFungibleTokenContractTable
   {
      get
      {
         return mongoDatabase.GetCollection<NonFungibleTokenContractTable>(SmartContractTableName);
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
