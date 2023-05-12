using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class GetMethodsLogReader : ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType is "TokenURI" or "RoyaltyInfo";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable) => new WriteModel<NonFungibleTokenTable>[]{};
}
