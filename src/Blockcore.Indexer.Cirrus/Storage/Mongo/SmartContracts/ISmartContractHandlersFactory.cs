using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ISmartContractHandlersFactory<T> where T : SmartContractComputedBase
{
   ILogReader<T> GetLogReader(string methodName);

   ISmartContractBuilder<T> GetSmartContractBuilder(string contractType);
}
