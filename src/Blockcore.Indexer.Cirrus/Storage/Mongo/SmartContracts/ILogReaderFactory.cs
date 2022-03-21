using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ILogReaderFactory
{
   ILogReader<T> GetLogReader<T>(string methodName) where T : SmartContractComputedBase;
}
