using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Cirrus.Storage;

public interface ICirrusStorage
{
   QueryContractCreate ContractCreate(string address);
   QueryResult<QueryContractCall> ContractCall(string address, string filterAddress, int? offset, int limit);
   QueryContractTransaction ContractTransaction(string transacitonId);
   QueryContractCode ContractCode(string address);
   QueryResult<QueryContractGroup> GroupedContracts();
   QueryResult<QueryContractList> ListContracts(string contractType, int? offset, int limit);

   Task<QueryResult<QueryAddressAsset>> GetAssetsForAddressAsync(string address, int? offset, int limit);
}
