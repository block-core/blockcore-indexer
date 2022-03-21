using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

public class GetOrPropertyLogReader : ILogReader<StandardTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.StartsWith("get_") ||
                                                             methodType.StartsWith("Get");

   public bool IsTheTransactionLogComplete(LogResponse[] logs) => true;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenComputedTable computedTable)
   {

   }
}
