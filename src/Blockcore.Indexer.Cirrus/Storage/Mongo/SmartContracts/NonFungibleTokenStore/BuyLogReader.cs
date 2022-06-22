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
      var transferLog = contractTransaction.Logs[0];
      var tokenPurchaseLog = contractTransaction.Logs[1];

      string seller = (string)tokenPurchaseLog.Log.Data["seller"];

      string tokenId = (string)transferLog.Log.Data["tokenId"];

      string buyer =(string)tokenPurchaseLog.Log.Data["buyer"];

      var updateInstruction = new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, buyer)
            .Set("SalesHistory.$[i].Buyer", buyer)
            .Set("SalesHistory.$[i].Sold", true)
            .Set("SalesHistory.$[i].PurchaseTransactionId", contractTransaction.TransactionId));

      updateInstruction.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<OnSale>(new BsonDocument("$and",
            new BsonArray(new[]
            {
               new BsonDocument("i.Seller", seller),
               new BsonDocument("i._t", "OnSale"),
               new BsonDocument("i.Sold", false)
            })))
      };


      return new[] { updateInstruction };
   }
}
