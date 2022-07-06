using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Storage.Mongo;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public class CirrusBlockRewindOperation : BlockRewindOperation, IBlockRewindOperation
{
   public CirrusBlockRewindOperation(ICirrusMongoDb storage) : base(storage)
   { }

   public new async Task RewindBlockAsync(uint blockIndex)
   {
      await base.RewindBlockAsync(blockIndex);

      var smartContractsToBeDeleted = ((ICirrusMongoDb)storage).SmartContractTable.AsQueryable()
         .Where(_ => _.LastProcessedBlockHeight >= blockIndex)
         .ToList();

      var rewindTasks = smartContractsToBeDeleted.Select(_ => DeleteTokensForSmartContractAddressAsync(_.ContractAddress));

      await Task.WhenAll(rewindTasks);
   }

   Task DeleteTokensForSmartContractAddressAsync(string address)
   {
      var nftTask = ((ICirrusMongoDb)storage).NonFungibleTokenTable
         .DeleteManyAsync(t => t.Id.ContractAddress == address);

      var tokenTask = ((ICirrusMongoDb)storage).DaoContractProposalTable
         .DeleteManyAsync(t => t.Id.ContractAddress == address);

      var daoTask = ((ICirrusMongoDb)storage).DaoContractProposalTable
         .DeleteManyAsync(t => t.Id.ContractAddress == address);

      return Task.WhenAll(nftTask,tokenTask, daoTask);
   }
}
