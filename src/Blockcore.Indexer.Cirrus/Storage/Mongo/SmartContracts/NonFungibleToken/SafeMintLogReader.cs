using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeMintLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SafeMint");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 3 };

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      LogData log = contractTransaction.Logs[0].Log;
      LogData saleLog = contractTransaction.Logs[1].Log;
      LogData uriLog = contractTransaction.Logs[2].Log;

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      computedTable.Tokens.Add(new()
      {
         Creator = contractTransaction.FromAddress,
         Owner = contractTransaction.FromAddress,
         Id = id,
         Uri = (string)uriLog.Data["tokenUri"],
         SalesHistory = new() { SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog, log) }
      });
   }


}
