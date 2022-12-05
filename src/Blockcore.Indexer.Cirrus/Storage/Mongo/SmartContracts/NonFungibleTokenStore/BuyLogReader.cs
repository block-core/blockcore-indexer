using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenStore;

public class BuyLogReader : ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Buy");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = contractTransaction.Logs.SingleOrDefault(_ => _.Log.Event == "TransferLog");
      var tokenPurchaseLog = contractTransaction.Logs.SingleOrDefault(_ => _.Log.Event == "TokenPurchasedLog");
      var royaltyPaidLog = contractTransaction.Logs.SingleOrDefault(_ => _.Log.Event == "RoyaltyPaidLog");

      string seller = tokenPurchaseLog?.Log.Data.Contains("seller") ?? false
         ? (string)tokenPurchaseLog.Log.Data["seller"]
         :  string.Empty;

      string tokenId = (string)transferLog.Log.Data["tokenId"];

      string buyer =(string)tokenPurchaseLog.Log.Data["buyer"];

      var updateInstruction = new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, buyer)
            .Set("salesHistory.$[i].buyer", buyer)
            .Set("salesHistory.$[i].sold", true)
            .Set("salesHistory.$[i].purchaseTransactionId", contractTransaction.TransactionId));


      var bsonFilterArray = new BsonArray(new[]
      {
         new BsonDocument("i._t", "onSale"),
         new BsonDocument("i.sold", false)
      });

      if (seller != string.Empty)
         bsonFilterArray.Add(new BsonDocument("i.seller", seller));

      updateInstruction.ArrayFilters = new[] { new BsonDocumentArrayFilterDefinition<OnSale>(
         new BsonDocument("$and",bsonFilterArray)) };

      if (royaltyPaidLog != null)
      {
         //TODO add the royalty data to the sale history
      }


      return new[] { updateInstruction };
   }
}
