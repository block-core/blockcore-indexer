using System.Threading.Tasks;

namespace Blockcore.Indexer.Core.Storage.Postgres;

public interface IBlockRewindOperation
{
   Task RewindBlockAsync(uint blockIndex);
}