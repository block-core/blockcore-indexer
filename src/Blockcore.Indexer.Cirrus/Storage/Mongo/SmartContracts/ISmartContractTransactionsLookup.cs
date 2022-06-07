using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface ISmartContractTransactionsLookup<T> where T : SmartContractComputedBase
{
   Task<List<CirrusContractTable>> GetTransactionsForSmartContractAsync(string address,
      long lastProcessedBlockHeight);
}
