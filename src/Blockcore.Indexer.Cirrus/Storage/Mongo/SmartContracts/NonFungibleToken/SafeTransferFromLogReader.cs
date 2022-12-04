using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeTransferFromLogReader : ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("SafeTransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(
      CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var log = contractTransaction.Logs.First().Log;
      var saleLog = contractTransaction.Logs.Last().Log;

      string tokenId = log.Data["tokenId"].ToString();

      if (contractTransaction.Logs.Any(_ => _.Log.Data.Contains("seller") ))
      {
         string owner = (string)saleLog.Data["seller"];

         var sale = SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog, log);

         return new[]
         {
            new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
                  .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
               Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, owner)
                  .AddToSet(_ => _.SalesHistory, sale))
         };
      }

      return new[]
      {
         new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
               .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, log.Data["to"].ToString())
               .AddToSet(_ => _.SalesHistory,
                  new OwnershipTransfer
                  {
                     From = log.Data["from"].ToString(),
                     TransactionId = contractTransaction.TransactionId,
                     To = log.Data["to"].ToString()
                  }))
      };
   }
}
