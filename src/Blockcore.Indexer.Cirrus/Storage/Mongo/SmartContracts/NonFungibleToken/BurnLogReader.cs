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
      long tokenId = (long)contractTransaction.Logs.SingleOrDefault().Log.Data["tokenId"];

      computedTable.Tokens.Single(_ => _.Id == tokenId)
         .IsBurned = true;
   }
}
