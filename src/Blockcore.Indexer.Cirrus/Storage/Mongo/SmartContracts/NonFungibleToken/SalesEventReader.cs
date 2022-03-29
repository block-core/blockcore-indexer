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
         Console.WriteLine(e);
         throw;
      }
   }

   static OnSale GetSaleDetails(string transactionId, LogData saleLog, LogData log) =>
      new()
      {
         Seller = (string)saleLog.Data["seller"],
         Price = (long)saleLog.Data["price"],
         TransactionId = transactionId
      };

   static Auction GetAuctionDetails(string transactionId, LogData saleLog, LogData log) =>
      new()
      {//TODO need to understand the auction logic and if it should have it's own reader
         Seller = (string)log.Data["from"],
         StartingPrice = (long)saleLog.Data["startingPrice"],
         EndBlock = (long)saleLog.Data["endBlock"],
         TransactionId = transactionId
      };
}
