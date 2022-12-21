using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class MintLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Mint");

   public override List<LogType> RequiredLogs { get; set; } = new() { LogType.TransferLog, LogType.MintExtract };

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var log = GetLogByType(LogType.TransferLog, contractTransaction.Logs).Log;
      var uriLog = GetLogByType(LogType.MintExtract, contractTransaction.Logs).Log;

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      return new[]
      {
         new InsertOneModel<NonFungibleTokenTable>(new NonFungibleTokenTable
         {
            Owner = log.Data["to"].ToString(),
            Id = new SmartContractTokenId { TokenId = id, ContractAddress = computedTable.ContractAddress },
            Uri = uriLog.Data["tokenUri"].ToString()
         })
      };
   }
}
