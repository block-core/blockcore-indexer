using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class BurnLogReader: ILogReader<NonFungibleTokenComputedTable,Types.NonFungibleToken>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Burn");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<Types.NonFungibleToken>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      object tokenId = contractTransaction.Logs.SingleOrDefault().Log.Data["tokenId"];

      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      // computedTable.Tokens.Single(_ => _.Id == id)
      //    .IsBurned = true;

      return new [] { new UpdateOneModel<Types.NonFungibleToken>(
         Builders<Types.NonFungibleToken>.Filter.Where(_ =>
            _.Id == id && _.SmartContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleToken>.Update.Set(_ => _.IsBurned, true))};
   }
}
