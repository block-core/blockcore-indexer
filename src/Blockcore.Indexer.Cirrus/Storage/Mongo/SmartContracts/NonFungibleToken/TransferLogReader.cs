using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class TransferLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public override List<string> SupportedMethods { get; } = new() { "TransferLog", "TransferFrom" };
   public override List<LogType> RequiredLogs { get; } = new() { LogType.TransferLog };

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transferLog = GetLogByType(LogType.TransferLog, contractTransaction.Logs);

      if (!transferLog.Log.Data.ContainsKey("tokenId"))
         throw new Exception($"token id not found in transfer log for {contractTransaction.TransactionId}");

      string tokenId = transferLog.Log.Data["tokenId"].ToString();

      string owner = transferLog.Log.Data["to"].ToString();

      var sale = new OwnershipTransfer
      {
         From = (string)transferLog.Log.Data["from"],
         To = owner,
         TransactionId = contractTransaction.TransactionId,
      };

      return new [] {new UpdateOneModel<NonFungibleTokenTable>(Builders<NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<NonFungibleTokenTable>.Update.Set(_ => _.Owner, owner)
            .AddToSet(_ => _.SalesHistory, sale)) };
   }
}
