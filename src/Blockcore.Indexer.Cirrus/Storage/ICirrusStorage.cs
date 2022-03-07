using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Cirrus.Storage;

public interface ICirrusStorage
{
   QueryContractCreate ContractCreate(string address);
   QueryResult<QueryContractCall> ContractCall(string address, string filterAddress, int? offset, int limit);
   QueryContractTransaction ContractTransaction(string transacitonId);
   QueryContractCode ContractCode(string address);
}
