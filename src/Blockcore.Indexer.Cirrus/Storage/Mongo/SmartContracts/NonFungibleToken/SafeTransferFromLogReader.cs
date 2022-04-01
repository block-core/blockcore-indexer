using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeTransferFromLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SafeTransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var log = contractTransaction.Logs.First().Log;
      var saleLog = contractTransaction.Logs.Last().Log;

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      var token = computedTable.Tokens.First(_ => _.Id == id);

      token.Owner = (string)saleLog.Data["seller"];

      token.SalesHistory.Add(SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog, log));
   }
}
