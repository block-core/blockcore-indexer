using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.NonFungibleTokenAuction;

public class AuctionEndLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("AuctionEnd");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var transferLog = contractTransaction.Logs[0];
      var auctionLog = contractTransaction.Logs[1];

      string tokenId = (string)transferLog.Log.Data["tokenId"];

      var token = computedTable.Tokens.Single(_ => _.Id == tokenId);

      var auctionEvent = (Auction) token.SalesHistory.Last(_ => _ is Auction);

      auctionEvent.Success = auctionLog.Log.Event switch
      {
         "AuctionEndFailedLog" => false,
         "AuctionEndSucceedLog" => true,
         _ => auctionEvent.Success
      };

      if (auctionEvent.Success)
         token.Owner = auctionEvent.HighestBidder;
   }
}
