using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeMintLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SafeMint");

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var log = contractTransaction.Logs[0]?.Log;
      var saleLog = contractTransaction.Logs[1]?.Log;
      var uriLog = contractTransaction.Logs[2]?.Log;

      if (log is null || saleLog is null || uriLog is null)
         throw new ArgumentNullException(nameof(log));

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      computedTable.Tokens.Add(new()
      {
         Creator = contractTransaction.FromAddress,
         Owner = (string)log.Data["to"],
         Id = id,
         Uri = (string)uriLog.Data["tokenUri"],
         SalesHistory = new() { SalesEventReader.SaleDetails(contractTransaction, saleLog, log) }
      });
   }


}
