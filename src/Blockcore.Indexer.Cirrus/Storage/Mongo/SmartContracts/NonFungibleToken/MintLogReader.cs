using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class MintLogReader : ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Mint");

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var log = contractTransaction.Logs.First()?.Log;
      var uriLog = contractTransaction.Logs.Last()?.Log;

      if (log is null || uriLog is null)
         throw new ArgumentNullException(nameof(log));

      object tokenId = log.Data["tokenId"];
      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      return new [] { new InsertOneModel<Types.NonFungibleToken>(new Types.NonFungibleToken
      {
         Owner = (string)log.Data["to"],
         Id = new SmartContractTokenId
         {
            TokenId = id,ContractAddress = computedTable.ContractAddress
         },
         Uri = (string)uriLog.Data["tokenUri"]
      })};
   }
}
