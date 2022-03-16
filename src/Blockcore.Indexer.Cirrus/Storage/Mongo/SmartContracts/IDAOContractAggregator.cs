using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface IDAOContractAggregator
{
   Task<DaoContractComputedTable> ComputeDaoContractForAddressAsync(string address);
}
