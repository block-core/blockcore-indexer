using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class WhitelistAddressesLogReader : ILogReader
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "WhitelistAddresses" || methodType == "WhitelistAddress";

   public bool IsTheTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      if (contractTransaction.Logs.Any() == false)
         return;

      computedTable.WhitelistedCount =  (long)contractTransaction.Logs.Single().Log.Data["whitelistedCount"];
      //TODO
   }
}
