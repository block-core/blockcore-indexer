using Blockcore.Indexer.Cirrus.Operations.Types;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public class CirrusStorageBatchFactory : IStorageBatchFactory
{
   public StorageBatch GetStorageBatch() => new CirrusStorageBatch();
}
