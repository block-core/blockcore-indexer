using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class PendingOwnerLogReader : ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SetPendingOwner");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var key = contractTransaction.Logs.SingleOrDefault()?.Log.Data.ContainsKey("PendingOwner") ?? false
         ? "PendingOwner"
         : contractTransaction.Logs.SingleOrDefault()?.Log.Data.ContainsKey("pendingOwner") ?? false
            ? "pendingOwner"
            : string.Empty;

      if (key != string.Empty)
      {
         computedTable.PendingOwner = (string)contractTransaction.Logs.SingleOrDefault()?.Log.Data[key] ?? string.Empty;
      }

      return null;
   }
}
