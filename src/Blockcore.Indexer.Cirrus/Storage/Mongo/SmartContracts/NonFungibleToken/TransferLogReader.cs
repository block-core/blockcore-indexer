using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class TransferLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferLog") || methodType.Equals("TransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var log = contractTransaction.Logs?.First();

      if (log is null)
         throw new ArgumentNullException(nameof(log));

      string tokenId = (string)log.Log.Data["tokenId"];

      var token = computedTable.Tokens.First(_ => _.Id == tokenId);

      token.Owner = (string)log.Log.Data["to"];

      token.SalesHistory.Add(new OwnershipTransfer
      {
         From = (string)log.Log.Data["from"],
         To = (string)log.Log.Data["to"],
         TransactionId = contractTransaction.TransactionId,
      });
   }
}
