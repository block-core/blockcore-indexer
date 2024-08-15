using System.Collections.Generic;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage
{
    public interface IMapPgBlockToStorageBlock
    {
        SyncBlockInfo Map(Block block);
        Block Map(BlockInfo blockInfo);
    }
}
