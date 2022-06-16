using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeTransferFromLogReader : ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SafeTransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var log = contractTransaction.Logs.First().Log;
      var saleLog = contractTransaction.Logs.Last().Log;

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      string owner = (string)saleLog.Data["seller"];

       var sale =SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog, log);

       return new [] { new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
             .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
          Builders<Types.NonFungibleToken>.Update.Set(_ => _.Owner, owner)
             .AddToSet(_ => _.SalesHistory, sale))};
   }
}
