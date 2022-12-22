using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class AuctionEndLogReader : ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   ICirrusMongoDb db;

   public AuctionEndLogReader(ICirrusMongoDb db)
   {
      this.db = db;
   }

   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("AuctionEnd");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs.Any(_ => _.Log.Event.Equals("TransferLog"))
   && logs.Any(_ => _.Log.Event.StartsWith("AuctionEnd"));

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = contractTransaction.Logs.First(_ => _.Log.Event.Equals("TransferLog"));
      var auctionLog = contractTransaction.Logs.First(_ => _.Log.Event.StartsWith("AuctionEnd"));

      string tokenId = transferLog.Log.Data["tokenId"].ToString();

      bool success = auctionLog.Log.Event switch
      {
         "AuctionEndFailedLog" => false,
         "AuctionEndSucceedLog" => true,
         _ => false
      };

      UpdateOneModel<Types.NonFungibleTokenTable> updateInstruction;

      if (success)
      {
         updateInstruction = new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
               .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<Types.NonFungibleTokenTable>.Update
               .Set(_ => _.Owner, (string)auctionLog.Log.Data["highestBidder"])
               .Set("SalesHistory.$[i].Success", true)
               .Set("SalesHistory.$[i].AuctionEnded", true));

         // updateInstruction.ArrayFilters = new[]
         // {
         //    new BsonDocumentArrayFilterDefinition<Auction>(
         //       new BsonDocument("$and", new BsonArray(
         //          new[]
         //          {
         //             new BsonDocument("i._t[1]", nameof(Auction)),
         //             new BsonDocument("i.HighestBidTransactionId",
         //                contractTransaction.TransactionId)
         //          })))
         // };
      }
      else
      {
         updateInstruction = new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
               .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<Types.NonFungibleTokenTable>.Update
               .Set("SalesHistory.$[i].AuctionEnded", true));
      }

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
