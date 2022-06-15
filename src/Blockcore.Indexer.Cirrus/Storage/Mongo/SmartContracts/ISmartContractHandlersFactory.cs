using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ISmartContractHandlersFactory<T,TDocument>
   where T : SmartContractComputedBase
   where TDocument : new()
{
   ILogReader<T, TDocument> GetLogReader(string methodName);

   ISmartContractBuilder<T> GetSmartContractBuilder(string contractType);
}
