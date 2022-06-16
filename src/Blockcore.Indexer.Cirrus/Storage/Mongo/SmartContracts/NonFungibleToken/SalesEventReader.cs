using System;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SalesEventReader
{
   public static TokenSaleEvent SaleDetails(string transactionId, LogData saleLog, LogData log)
   {
      try
      {
         return saleLog.Event switch
         {
            "TokenOnSaleLog" => GetSaleDetails(transactionId, saleLog, log),
            "AuctionStartedLog" => GetAuctionDetails(transactionId, saleLog, log),
            _ => null
         };
      }
      catch (Exception e)
      {
         throw;
      }
   }

   private static OnSale GetSaleDetails(string transactionId, LogData saleLog, LogData log) =>
      new()
      {
         Seller = (string)saleLog.Data["seller"],
         Price = (long)saleLog.Data["price"],
         TransactionId = transactionId
      };

   private static Auction GetAuctionDetails(string transactionId, LogData saleLog, LogData log) =>
      new()
      {
         Seller = (string)log.Data["from"],
         StartingPrice = (long)saleLog.Data["startingPrice"],
         EndBlock = (long)saleLog.Data["endBlock"],
         TransactionId = transactionId
      };
}
