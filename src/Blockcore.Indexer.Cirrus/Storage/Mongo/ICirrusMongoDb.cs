using System;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public interface ICirrusMongoDb : IMongoDb
{
   public IMongoCollection<CirrusContractTable> CirrusContractTable { get;}
   public IMongoCollection<CirrusContractCodeTable> CirrusContractCodeTable { get; }
   public IMongoCollection<DaoContractComputedTable> DaoContractComputedTable { get; }
   public IMongoCollection<StandardTokenComputedTable> StandardTokenComputedTable { get; }
   public IMongoCollection<NonFungibleTokenComputedTable> NonFungibleTokenComputedTable { get; }


   public IMongoCollection<NonFungibleToken> NonFungibleTokenTable { get; }
   public IMongoCollection<DaoContractProposal> DaoContractProposalTable{ get; }

   public IMongoCollection<StandardTokenHolder> StandardTokenHolderTable{ get; }
}
