using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class MintLogReader : ILogReader<NonFungibleTokenContractTable, Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Mint");

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<Types.NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      var log = contractTransaction.Logs.First()?.Log;
      var uriLog = contractTransaction.Logs.Last()?.Log;

      if (log is null || uriLog is null)
         throw new ArgumentNullException(nameof(log));

      string tokenId = log.Data["tokenId"].ToString();

      return new[] { new InsertOneModel<Types.NonFungibleTokenTable>(new Types.NonFungibleTokenTable
      {
         Owner = (string)log.Data["to"],
         Id = new SmartContractTokenId
         {
            TokenId = tokenId,ContractAddress = computedTable.ContractAddress
         },
         Uri = (string)uriLog.Data["tokenUri"]
      })};
   }
}
