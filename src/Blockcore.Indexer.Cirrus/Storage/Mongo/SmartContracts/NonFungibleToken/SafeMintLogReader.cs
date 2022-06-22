using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeMintLogReader : ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SafeMint");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 3 };

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      LogData log = contractTransaction.Logs[0].Log;
      LogData saleLog = contractTransaction.Logs[1].Log;
      LogData uriLog = contractTransaction.Logs[2].Log;

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      return new [] {new InsertOneModel<Types.NonFungibleTokenTable>(new()
      {
         Creator = contractTransaction.FromAddress,
         Owner = contractTransaction.FromAddress,
         Id = new SmartContractTokenId
         {
            TokenId = id,ContractAddress = computedTable.ContractAddress
         },
         Uri = (string)uriLog.Data["tokenUri"],
         SalesHistory = new() { SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog, log) }
      })};
   }


}
