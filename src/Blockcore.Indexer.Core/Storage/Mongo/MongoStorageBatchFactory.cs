using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public class MongoStorageBatchFactory : IStorageBatchFactory
{
   public StorageBatch GetStorageBatch() => new MongoStorageBatch();
}
