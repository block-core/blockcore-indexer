using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ISmartContractBuilder<T>
where T : SmartContractTable
{
   bool CanBuildSmartContract(string contractCodeType);

   T BuildSmartContract(CirrusContractTable createContractTransaction);
}
