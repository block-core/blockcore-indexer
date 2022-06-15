using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ILogReader<in T, TDocument> where T : SmartContractComputedBase
where TDocument : new()
{
   bool CanReadLogForMethodType(string methodType);
   bool IsTransactionLogComplete(LogResponse[] logs);
   WriteModel<TDocument>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,T computedTable);
}
