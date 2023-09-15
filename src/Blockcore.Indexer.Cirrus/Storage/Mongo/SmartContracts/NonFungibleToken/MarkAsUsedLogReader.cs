using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class MarkAsUsedLogReader : ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("MarkAsUsed");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs?.Any(_ => _.Log.Event == "MarkAsUsedLog") ?? false;

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var log = contractTransaction.Logs.FirstOrDefault(_ => _.Log.Event == "MarkAsUsedLog")?.Log
         ?? throw new InvalidOperationException($"missing MarkAsUsedLog in MarkAsRead transaction - {contractTransaction.TransactionId}");

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      return new[]
      {
         new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
               .Where(_ => _.Id.TokenId == id && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<NonFungibleTokenTable>.Update.Set(_ => _.IsUsed, true))
      };
   }
}
