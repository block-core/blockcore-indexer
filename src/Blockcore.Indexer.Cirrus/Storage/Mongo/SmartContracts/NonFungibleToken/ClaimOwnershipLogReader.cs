using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class ClaimOwnershipLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("ClaimOwnership");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      computedTable.Owner = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["NewOwner"];
      computedTable.PreviousOwners.Add((string)contractTransaction.Logs.SingleOrDefault().Log.Data["PreviousOwner"]);

   }
}
