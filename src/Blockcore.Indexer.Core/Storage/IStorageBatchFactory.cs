using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Storage;

public interface IStorageBatchFactory
{
   StorageBatch GetStorageBatch();
}
