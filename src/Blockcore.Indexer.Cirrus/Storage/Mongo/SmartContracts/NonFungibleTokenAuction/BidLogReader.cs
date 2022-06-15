using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class BidLogReader : ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Bid");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var auctionLog = contractTransaction.Logs?[0];

     string tokenId = (string)auctionLog.Log.Data["tokenId"];

      //var token = computedTable.Tokens.Single(_ => _.Id == tokenId);

      // var auctionEvent = (Auction) token.SalesHistory.Last(_ => _ is Auction);
      //
      // auctionEvent.HighestBid = (long)auctionLog.Log.Data["bid"];
      // auctionEvent.HighestBidder = (string)auctionLog.Log.Data["bidder"];
      // auctionEvent.HighestBidTransactionId = contractTransaction.TransactionId;

      return null; //TODO lookup the auction event and than update
   }
}
