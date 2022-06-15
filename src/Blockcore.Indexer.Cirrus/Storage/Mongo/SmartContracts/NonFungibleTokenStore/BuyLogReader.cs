using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenStore;

public class BuyLogReader : ILogReader<NonFungibleTokenComputedTable, Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Buy");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var transferLog = contractTransaction.Logs[0];
      var tokenPurchaseLog = contractTransaction.Logs[1];

      string seller = (string)tokenPurchaseLog.Log.Data["seller"];

      string tokenId = (string)transferLog.Log.Data["tokenId"];

     //var token = computedTable.Tokens.Single(_ => _.Id == tokenId);

      // token.Owner = (string)tokenPurchaseLog.Log.Data["buyer"];
      //
      // var onSale = (OnSale)token.SalesHistory.Last(_ => _ is OnSale sale
      //                                                   && sale.Seller == seller
      //                                                   && !sale.Sold);
      //
      //
      // onSale.Buyer = (string)tokenPurchaseLog.Log.Data["buyer"];
      // onSale.Sold = true;
      // onSale.PurchaseTransactionId = contractTransaction.TransactionId;

      string buyer =(string)tokenPurchaseLog.Log.Data["buyer"];

      var updateInstruction = new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
            .Where(_ => _.Id == tokenId && _.SmartContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleToken>.Update.Set(_ => _.Owner, buyer)
            .Set("SalesHistory.$[i]", buyer)
            .Set("SalesHistory.$[i]", true)
            .Set("SalesHistory.$[i]", contractTransaction.TransactionId));

      updateInstruction.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<OnSale>(new BsonDocument("i.Seller", seller)),
         new BsonDocumentArrayFilterDefinition<OnSale>(new BsonDocument("i._type", "OnSale")),
         new BsonDocumentArrayFilterDefinition<OnSale>(new BsonDocument("i.Sold", false))
      };


      return new[] { updateInstruction };
   }
}
