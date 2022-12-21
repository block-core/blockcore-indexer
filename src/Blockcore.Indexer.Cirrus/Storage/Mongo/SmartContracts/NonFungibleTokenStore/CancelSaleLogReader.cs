using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenStore;

public class CancelSaleLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>
{
   public override List<string> SupportedMethods { get; } = new() { "CancelSale" };
   public override List<LogType> RequiredLogs { get; } = new() { LogType.TransferLog };

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = GetLogByType(LogType.TransferLog,contractTransaction.Logs);

      string tokenId = transferLog.Log.Data["tokenId"].ToString();

      var updateInstruction = new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Unset("SalesHistory.$[i]"));

      updateInstruction.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<OnSale>(new BsonDocument("$and",
            new BsonArray(new[]
            {
               new BsonDocument("i.Seller", contractTransaction.FromAddress),
               new BsonDocument("i._t", "OnSale"),
               new BsonDocument("i.Sold", false)
            })))
      };

      return new[] { updateInstruction };
   }
}
