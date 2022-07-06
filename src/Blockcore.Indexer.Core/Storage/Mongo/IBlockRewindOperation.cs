using System.Threading.Tasks;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public interface IBlockRewindOperation
{
   Task RewindBlockAsync(uint blockIndex);
}
