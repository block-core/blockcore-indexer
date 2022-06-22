using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class TransferLogReader : ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferLog") || methodType.Equals("TransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var log = contractTransaction.Logs?.First();

      if (log is null)
         throw new ArgumentNullException(nameof(log));

      string tokenId = (string)log.Log.Data["tokenId"];

      //var token = computedTable.Tokens.First(_ => _.Id == tokenId);

      string owner = (string)log.Log.Data["to"];

      var sale = new OwnershipTransfer
      {
         From = (string)log.Log.Data["from"],
         To = (string)log.Log.Data["to"],
         TransactionId = contractTransaction.TransactionId,
      };

      return new [] {new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, owner)
            .AddToSet(_ => _.SalesHistory, sale)) };
   }
}
