using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class BidLogReader : ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   ICirrusMongoDb db;

   public BidLogReader(ICirrusMongoDb db)
   {
      this.db = db;
   }

   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Bid");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var auctionLog = contractTransaction.Logs?[0];

      string tokenId = (string)auctionLog.Log.Data["tokenId"];

      UpdateOneModel<Types.NonFungibleToken> updateInstruction;

      updateInstruction = new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleToken>.Update
            .Set("SalesHistory.$[i].HighestBid", (long)auctionLog.Log.Data["bid"])
            .Set("SalesHistory.$[i].HighestBidder", (string)auctionLog.Log.Data["bidder"])
            .Set("SalesHistory.$[i].HighestBidTransactionId", contractTransaction.TransactionId));

      updateInstruction.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<Auction>(
            new BsonDocument("$and", new BsonArray(
               new[]
               {
                  new BsonDocument("i._t", nameof(Auction)),
                  new BsonDocument("i.AuctionEnded",false)
               })))
      };

      return new[] { updateInstruction };
   }
}
