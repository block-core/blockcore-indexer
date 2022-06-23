using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface IComputeSmartContractService<T> where T : SmartContractTable
{
   Task ComputeSmartContractForAddressAsync(string address);
}
