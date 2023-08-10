using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class InterFluxGeMethodsLogReader : ILogReader<NonFungibleTokenContractTable, NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType is "BalanceOf" or "OwnerOf" or "GetApproved"
   or "IsApprovedForAll" or "TokenURI";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)=> new WriteModel<NonFungibleTokenTable>[]{};
}
