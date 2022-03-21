using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ILogReaderFactory<in T> where T : SmartContractComputedBase
{
   ILogReader<T> GetLogReader(string methodName);
}
