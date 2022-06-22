using System;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public interface ICirrusMongoDb : IMongoDb
{
   public IMongoCollection<CirrusContractTable> CirrusContractTable { get;}
   public IMongoCollection<CirrusContractCodeTable> CirrusContractCodeTable { get; }
   public IMongoCollection<DaoContractTable> DaoContractComputedTable { get; }
   public IMongoCollection<StandardTokenContractTable> StandardTokenComputedTable { get; }
   public IMongoCollection<NonFungibleTokenContractTable> NonFungibleTokenComputedTable { get; }


   public IMongoCollection<NonFungibleTokenTable> NonFungibleTokenTable { get; }
   public IMongoCollection<DaoContractProposalTable> DaoContractProposalTable{ get; }

   public IMongoCollection<StandardTokenHolderTable> StandardTokenHolderTable{ get; }
}
