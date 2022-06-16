using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class AuctionEndLogReader : ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   ICirrusMongoDb db;

   public AuctionEndLogReader(ICirrusMongoDb db)
   {
      this.db = db;
   }

   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("AuctionEnd");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var transferLog = contractTransaction.Logs[0];
      var auctionLog = contractTransaction.Logs[1];

      string tokenId = (string)transferLog.Log.Data["tokenId"];

      bool success = auctionLog.Log.Event switch
      {
         "AuctionEndFailedLog" => false,
         "AuctionEndSucceedLog" => true,
         _ => false
      };

      UpdateOneModel<Types.NonFungibleToken> updateInstruction;

      if (success)
      {
         updateInstruction = new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
               .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<Types.NonFungibleToken>.Update
               .Set(_ => _.Owner, (string)auctionLog.Log.Data["highestBidder"])
               .Set("SalesHistory.$[i].Success", true)
               .Set("SalesHistory.$[i].AuctionEnded", true));
      }
      else
      {
         updateInstruction = new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
               .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<Types.NonFungibleToken>.Update
               .Set("SalesHistory.$[i].AuctionEnded", true));
      }

      updateInstruction.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<Auction>(
            new BsonDocument("$and", new BsonArray(
               new[]
               {
                  new BsonDocument("i._t[1]", nameof(Auction)),
                  new BsonDocument("i.HighestBidTransactionId",
                     contractTransaction.TransactionId)
               })))
      };

      return new[] { updateInstruction };
   }
}
