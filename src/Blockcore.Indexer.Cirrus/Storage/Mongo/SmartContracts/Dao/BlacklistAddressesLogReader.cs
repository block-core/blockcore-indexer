using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class BlacklistAddressesLogReader : ILogReader<DaoContractComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType is "BlacklistAddresses" or "BlacklistAddress";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      //TODO
   }
}
