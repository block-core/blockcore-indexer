using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public interface IDAOContractAggregator
{
   Task<DaoContractComputedTable> ComputeDaoContractForAddressAsync(string address);
}
