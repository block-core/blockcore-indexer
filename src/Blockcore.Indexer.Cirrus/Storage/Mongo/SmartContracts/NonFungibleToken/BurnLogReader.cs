using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;
#pragma warning disable CS0253 // Possible unintended reference comparison; right hand side needs cast

public class BurnLogReader: ILogReader<NonFungibleTokenContractTable,Types.NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Burn");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      string tokenId = contractTransaction.Logs.SingleOrDefault().Log.Data["tokenId"].ToString();

      //string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      // computedTable.Tokens.Single(_ => _.Id == id)
      //    .IsBurned = true;

      return new [] { new UpdateOneModel<Types.NonFungibleTokenTable>(
         Builders<Types.NonFungibleTokenTable>.Filter
            .Where(_ => _.Id.TokenId == tokenId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<Types.NonFungibleTokenTable>.Update.Set(_ => _.IsBurned, true))};
   }
}
