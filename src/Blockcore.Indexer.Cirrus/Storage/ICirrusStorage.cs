using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
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
   QueryResult<QueryBlockSmartContractsLogs> ListBLocksLogs(long startBlock, long endBlock, int? offset, int limit);

   Task<QueryDAOContract> GetDaoContractByAddressAsync(string contractAddress);
   Task<QueryStandardTokenContract> GetStandardTokenContractByAddressAsync(string contractAddress);
   Task<QueryNonFungibleTokenContract> GetNonFungibleTokenContractByAddressAsync(string contractAddress);
   Task<NonFungibleTokenTable> GetNonFungibleTokenByIdAsync(string contractAddress, string tokenId);
   Task<QueryStandardToken> GetStandardTokenByIdAsync(string contractAddress, string tokenId);
   Task<QueryResult<QueryAddressAsset>> GetNonFungibleTokensForAddressAsync(string address, int? offset, int limit);
   Task<QueryResult<QueryStandardToken>> GetStandardTokensForAddressAsync(string address, int? offset, int limit);
   Task<List<string>> GetSmartContractsThatNeedsUpdatingAsync(params  string[] supportedTypes);
}
