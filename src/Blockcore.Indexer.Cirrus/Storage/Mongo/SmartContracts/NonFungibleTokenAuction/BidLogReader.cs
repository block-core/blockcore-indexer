using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class BidLogReader : ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   ICirrusMongoDb db;

   public BidLogReader(ICirrusMongoDb db)
   {
      this.db = db;
   }

   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Bid");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var auctionLog = contractTransaction.Logs?[0];

      string tokenId = (string)auctionLog.Log.Data["tokenId"];

      UpdateOneModel<Types.NonFungibleTokenTable> updateInstruction;

      updateInstruction = new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update
            .Set("SalesHistory.$[i].HighestBid", auctionLog.Log.Data["bid"].ToInt64())
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
