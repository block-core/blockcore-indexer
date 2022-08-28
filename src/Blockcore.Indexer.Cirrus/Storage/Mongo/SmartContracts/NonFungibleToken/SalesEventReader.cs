using System;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SalesEventReader
{
   public static TokenSaleEvent SaleDetails(string transactionId, LogData detailsLog, LogData log)
   {
      try
      {
         return detailsLog.Event switch
         {
            "TokenOnSaleLog" => GetSaleDetails(transactionId, detailsLog, log),
            "AuctionStartedLog" => GetAuctionDetails(transactionId, detailsLog, log),
            _ => null
         };
      }
      catch (Exception)
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

   private static Auction GetAuctionDetails(string transactionId, LogData auctionLog, LogData log) =>
      new()
      {
         Seller = (string)auctionLog.Data["seller"],
         StartingPrice = (long)auctionLog.Data["startingPrice"],
         EndBlock = (long)auctionLog.Data["endBlock"],
         TransactionId = transactionId
      };
}
