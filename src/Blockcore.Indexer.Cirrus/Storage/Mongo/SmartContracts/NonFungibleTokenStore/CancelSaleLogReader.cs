using System;
using System.Linq;
using System.Security.AccessControl;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenStore;

public class CancelSaleLogReader : ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("CancelSale");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = contractTransaction.Logs[0];
      var tokenPurchaseLog = contractTransaction.Logs[1];

      string tokenId = (string) transferLog.Log.Data["tokenId"];

      //var token = computedTable.Tokens.Single(_ => _.Id == tokenId);

      // var saleEvent = (OnSale) token.SalesHistory.Last(_ => _ is OnSale);
      //
      // if (saleEvent.Seller != contractTransaction.FromAddress)
      //    throw new InvalidOperationException($"The seller must cancel the sale order {saleEvent.TransactionId} for {contractTransaction.TransactionId}");

      //token.SalesHistory.Remove(saleEvent);

      var updateInstruction = new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Unset("salesHistory.$[i]"));

      updateInstruction.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<OnSale>(new BsonDocument("$and",
            new BsonArray(new[]
            {
               new BsonDocument("i.seller", contractTransaction.FromAddress),
               new BsonDocument("i._t", "onSale"),
               new BsonDocument("i.sold", false)
            })))
      };

      return new[] { updateInstruction };
   }
}
