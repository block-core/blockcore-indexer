using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class TransferLogReader : ILogReader<NonFungibleTokenComputedTable, Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferLog") || methodType.Equals("TransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
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

      return new [] {new UpdateOneModel<Types.NonFungibleToken>(Builders<Types.NonFungibleToken>.Filter
            .Where(_ => _.Id == tokenId && _.SmartContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleToken>.Update.Set(_ => _.Owner, owner)
            .AddToSet(_ => _.SalesHistory, sale)) };
   }
}
