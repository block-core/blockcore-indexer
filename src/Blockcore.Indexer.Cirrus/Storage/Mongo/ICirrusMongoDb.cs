using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public interface ICirrusMongoDb : IMongoDb
{
   public IMongoCollection<SmartContractTable> SmartContractTable { get; }
   public IMongoCollection<CirrusContractTable> CirrusContractTable { get;}
   public IMongoCollection<CirrusContractCodeTable> CirrusContractCodeTable { get; }
   public IMongoCollection<DaoContractTable> DaoContractTable { get; }
   public IMongoCollection<StandardTokenContractTable> StandardTokenContractTable { get; }
   public IMongoCollection<NonFungibleTokenContractTable> NonFungibleTokenContractTable { get; }


   public IMongoCollection<DaoContractProposalTable> DaoContractProposalTable{ get; }
   public IMongoCollection<NonFungibleTokenTable> NonFungibleTokenTable { get; }
   public IMongoCollection<StandardTokenHolderTable> StandardTokenHolderTable{ get; }
}
