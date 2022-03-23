using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class BlacklistAddressesLogReader : ILogReader
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "BlacklistAddresses" || methodType == "BlacklistAddress";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      //TODO
   }
}
