using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage.Postgres.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres;

public class PostgresStorageBatchFactory : IStorageBatchFactory
{
    public StorageBatch GetStorageBatch() => new PostgresStorageBatch();
}