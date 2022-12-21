using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SafeMintLogReader : LogReaderBase,ILogReader<NonFungibleTokenContractTable, NonFungibleTokenTable>
{
   public override List<string> SupportedMethods { get; } = new() { "SafeMint" };
   public override List<LogType> RequiredLogs { get; }
   public override bool IsTransactionLogComplete(LogResponse[] logs) => logs.Any(_ => _.Log.Event.Equals("TransferLog")) &&
                                                                        logs.Any(_ => _.Log.Event.Equals("MintExtract")) &&
                                                                        logs.Any(_ =>
                                                                           _.Log.Event.Equals("TokenOnSaleLog") ||
                                                                           _.Log.Event.Equals("AuctionStartedLog"));

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      LogData log = GetLogByType(LogType.TransferLog,contractTransaction.Logs).Log;
      LogData saleLog = contractTransaction.Logs.FirstOrDefault(_ => _.Log.Data.ContainsKey("seller"))?.Log;
      LogData uriLog = GetLogByType(LogType.MintExtract,contractTransaction.Logs).Log;

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      return new [] {new InsertOneModel<NonFungibleTokenTable>(new()
      {
         Creator = contractTransaction.FromAddress,
         Owner = contractTransaction.FromAddress,
         Id = new SmartContractTokenId
         {
            TokenId = id,ContractAddress = computedTable.ContractAddress
         },
         Uri = uriLog.Data["tokenUri"].ToString(),
         SalesHistory = new() { SalesEventReader.SaleDetails(contractTransaction.TransactionId, saleLog, log) }
      })};
   }


}
