using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenAuction;

public class BidLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   ICirrusMongoDb db;

   public BidLogReader(ICirrusMongoDb db)
   {
      this.db = db;
   }

   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Bid");

   public override List<LogType> RequiredLogs { get; set; } = new() { LogType.HighestBidUpdatedLog };

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var auctionLog = GetLogByType(LogType.HighestBidUpdatedLog,contractTransaction.Logs);

      string tokenId = auctionLog.Log.Data["tokenId"].ToString();

      UpdateOneModel<NonFungibleTokenTable> updateInstruction = new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update
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
