using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class BurnLogReader: LogReaderBase,ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Burn");

   public override List<LogType> RequiredLogs { get; set; } = new() { LogType.TransferLog };

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var transgerLog = GetLogByType(LogType.TransferLog, contractTransaction.Logs);
      string tokenId = transgerLog.Log.Data["tokenId"].ToString();

      return new [] { new UpdateOneModel<NonFungibleTokenTable>(
         Builders<NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<NonFungibleTokenTable>.Update.Set(_ => _.IsBurned, true))};
   }
}
