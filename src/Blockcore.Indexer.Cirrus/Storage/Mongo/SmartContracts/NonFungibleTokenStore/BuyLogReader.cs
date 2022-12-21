using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using FASTER.core;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenStore;

public class BuyLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public override List<string> SupportedMethods { get; } = new() { "Buy" };
   public override List<LogType> RequiredLogs { get; } = new()
   {
      LogType.TransferLog, LogType.TokenPurchasedLog
   };
   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = GetLogByType(LogType.TransferLog, contractTransaction.Logs);
      var tokenPurchaseLog = GetLogByType(LogType.TokenPurchasedLog, contractTransaction.Logs);
      var royaltyPaidLog = GetLogByType(LogType.RoyaltyPaidLog, contractTransaction.Logs);

      string seller = tokenPurchaseLog?.Log.Data.ContainsKey("seller") ?? false
         ? tokenPurchaseLog.Log.Data["seller"].ToString()
         :  string.Empty;

      string tokenId = transferLog.Log.Data["tokenId"].ToString();

      string buyer = tokenPurchaseLog.Log.Data["buyer"].ToString();

      var updateInstruction = new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, buyer)
            .Set("SalesHistory.$[i].Buyer", buyer)
            .Set("SalesHistory.$[i].Sold", true)
            .Set("SalesHistory.$[i].PurchaseTransactionId", contractTransaction.TransactionId));


      var bsonFilterArray = new BsonArray(new[]
      {
         new BsonDocument("i._t", "OnSale"),
         new BsonDocument("i.Sold", false)
      });

      if (seller != string.Empty)
         bsonFilterArray.Add(new BsonDocument("i.Seller", seller));

      updateInstruction.ArrayFilters = new[] { new BsonDocumentArrayFilterDefinition<OnSale>(
         new BsonDocument("$and",bsonFilterArray)) };

      if (royaltyPaidLog != null)
      {
         //TODO add the royalty data to the sale history
      }


      return new[] { updateInstruction };
   }
}
