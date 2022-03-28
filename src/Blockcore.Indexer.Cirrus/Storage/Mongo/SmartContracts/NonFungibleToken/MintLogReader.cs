using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class MintLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Mint") || methodType.Equals("SafeMint");

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var log = contractTransaction.Logs.First()?.Log;
      var uriLog = contractTransaction.Logs.Last()?.Log;

      if (log is null || uriLog is null)
         throw new ArgumentNullException(nameof(log));

      computedTable.Tokens.Add(new Token
      {
         Address = (string)log.Data["to"],
         Id = (long)log.Data["tokenId"],
         Uri = (string)uriLog.Data["tokenUri"]
      });
   }
}
