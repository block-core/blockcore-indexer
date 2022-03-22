using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public interface ILogReader
{
   bool CanReadLogForMethodType(string methodType);
   bool IsTransactionLogComplete(LogResponse[] logs);
   void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,DaoContractComputedTable computedTable);
}
