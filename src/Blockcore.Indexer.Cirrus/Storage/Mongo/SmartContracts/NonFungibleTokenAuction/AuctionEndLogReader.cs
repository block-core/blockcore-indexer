using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Microsoft.AspNetCore.Server.IIS.Core;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class AuctionEndLogReader : ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("AuctionEnd");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var transferLog = contractTransaction.Logs[0];
      var auctionLog = contractTransaction.Logs[1];

      string tokenId = (string)transferLog.Log.Data["tokenId"];

      //var token = computedTable.Tokens.Single(_ => _.Id == tokenId);

      //var auctionEvent = (Auction) token.SalesHistory.Last(_ => _ is Auction);

      bool success = auctionLog.Log.Event switch
      {
         "AuctionEndFailedLog" => false,
         "AuctionEndSucceedLog" => true,
         _ => false
      };
      if (success)
      {
         return new [] {new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
               .Where(_ => _.Id == tokenId && _.SmartContractAddress == computedTable.ContractAddress),
            Builders<Types.NonFungibleToken>.Update
               .Set(_ => _.Owner, "") //TODO David solve this issue !!!
               .Set(_ => ((Auction)_.SalesHistory.FindLast(p => p is Auction)).Success, true))};
      }

      return new [] {new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
            .Where(_ => _.Id == tokenId && _.SmartContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleToken>.Update
            .Set(_ => ((Auction)_.SalesHistory.FindLast(p => p is Auction)).Success, false))};
   }
}
