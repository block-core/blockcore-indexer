using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface IComputeSmartContractService<T> where T : SmartContractComputedBase
{
   Task<T> ComputeSmartContractForAddressAsync(string address);
}
