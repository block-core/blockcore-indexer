using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class PendingOwnerLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SetPendingOwner");

   public override List<LogType> RequiredLogs { get; set; }
   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      computedTable.PendingOwner = (string)contractTransaction.Logs.SingleOrDefault()?.Log.Data["PendingOwner"] ?? string.Empty;

      return null;
   }
}
