using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class ClaimOwnershipLogReader : ILogReader<NonFungibleTokenContractTable, NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("ClaimOwnership");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      computedTable.Owner = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["NewOwner"];
      computedTable.PreviousOwners.Add((string)contractTransaction.Logs.SingleOrDefault().Log.Data["PreviousOwner"]);

      return null;
   }
}
