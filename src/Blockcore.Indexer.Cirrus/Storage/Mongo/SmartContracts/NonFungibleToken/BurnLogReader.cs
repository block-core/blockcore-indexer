using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class BurnLogReader: ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Burn");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      object tokenId = contractTransaction.Logs.SingleOrDefault().Log.Data["tokenId"];

      string id = tokenId is string ? (string)tokenId : Convert.ToString(tokenId);

      computedTable.Tokens.Single(_ => _.Id == id)
         .IsBurned = true;
   }
}
