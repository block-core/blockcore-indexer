using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Sync.SyncTasks;

public interface ISmartContractServiceFactory
{

}

//  class SmartContractServiceFactory<T> : ISmartContractServiceFactory where T : SmartContractTable
//  {
//     Dictionary<string, IComputeSmartContractService<T>> dictionary;
//
//     public SmartContractServiceFactory(IComputeSmartContractService<DaoContractTable> daoContractService, IComputeSmartContractService<StandardTokenContractTable> standardTokenService, IComputeSmartContractService<NonFungibleTokenContractTable> nonFungibleTokenService)
//     {
//        dictionary = new ()
//        {
//           { "DAOContract", (IComputeSmartContractService<T>) daoContractService },
//           { "StandardToken", (IComputeSmartContractService<T>) standardTokenService },
//           { "NonFungibleToken", (IComputeSmartContractService<T>) nonFungibleTokenService },
//        };
//     }
//
//     public IComputeSmartContractService<T> getComputeSmartContractServiceForAddress(string address)
//     {
//
//     }
// }
