using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeTransferFromLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   public override List<string> SupportedMethods { get; } = new() { "SafeTransferFrom" };
   public override List<LogType> RequiredLogs { get; }

   public override bool IsTransactionLogComplete(LogResponse[] logs) => logs.Any(_ => _.Log.Event.Equals("TransferLog"))
                                                                        && logs.Any(_ =>
                                                                           _.Log.Event.Equals("TokenOnSaleLog") ||
                                                                           _.Log.Event.Equals("AuctionStartedLog"));

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(
      CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = GetLogByType(LogType.TransferLog,contractTransaction.Logs).Log;
      var saleLog = contractTransaction.Logs.FirstOrDefault(_ => _.Log.Data.ContainsKey("seller"));

      string tokenId = transferLog.Data["tokenId"].ToString();

      if (saleLog != null)
      {
         string owner = saleLog.Log.Data["seller"].ToString();

         var sale = SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog.Log, transferLog);

         return new[]
         {
            new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
                  .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
               Builders<NonFungibleTokenTable>.Update.Set(_ => _.Owner, owner)
                  .AddToSet(_ => _.SalesHistory, sale))
         };
      }

      return new[]
      {
         new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
               .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<NonFungibleTokenTable>.Update.Set(_ => _.Owner, transferLog.Data["to"].ToString())
               .AddToSet(_ => _.SalesHistory,
                  new OwnershipTransfer
                  {
                     From = transferLog.Data["from"].ToString(),
                     TransactionId = contractTransaction.TransactionId,
                     To = transferLog.Data["to"].ToString()
                  }))
      };
   }
}
