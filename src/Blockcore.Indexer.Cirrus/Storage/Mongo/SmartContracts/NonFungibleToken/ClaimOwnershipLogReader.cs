using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class ClaimOwnershipLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable, NonFungibleTokenTable>
{
   public override List<string> SupportedMethods { get; } = new() { "ClaimOwnership" };
   public override List<LogType> RequiredLogs { get; }

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      computedTable.Owner = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["NewOwner"];
      computedTable.PreviousOwners.Add((string)contractTransaction.Logs.SingleOrDefault().Log.Data["PreviousOwner"]);

      return null;
   }
}
