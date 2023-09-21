using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class TransferLogReader : ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferLog") ||
                                                             methodType.Equals("TransferFrom") ||
                                                             methodType.Equals("DelegatedTransfer");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs.Any(_ => _.Log.Event.Equals("TransferLog"));

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = contractTransaction.Logs?.First(_ => _.Log.Event.Equals("TransferLog"));

      if (transferLog is null)
         throw new ArgumentNullException(nameof(transferLog));

      if (!transferLog.Log.Data.ContainsKey("tokenId"))
         throw new Exception($"token id not found in transfer log for {contractTransaction.TransactionId}");

      string tokenId = transferLog.Log.Data["tokenId"]?.ToString();

      //var token = computedTable.Tokens.First(_ => _.Id == tokenId);

      string owner = transferLog.Log.Data["to"].ToString();

      var sale = new OwnershipTransfer
      {
         From = (string)transferLog.Log.Data["from"],
         To = owner,
         TransactionId = contractTransaction.TransactionId,
      };

      return new [] {new UpdateOneModel<Types.NonFungibleTokenTable>(Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.Owner, owner)
            .AddToSet(_ => _.SalesHistory, sale)) };
   }
}
