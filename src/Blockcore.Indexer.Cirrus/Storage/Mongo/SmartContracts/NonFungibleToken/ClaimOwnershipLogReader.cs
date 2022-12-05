using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class ClaimOwnershipLogReader : ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("ClaimOwnership");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      computedTable.Owner = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["newOwner"];
      computedTable.PreviousOwners.Add((string)contractTransaction.Logs.SingleOrDefault().Log.Data["previousOwner"]);

      return null;
   }
}
