using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage
{
   public interface IMapMongoBlockToStorageBlock
   {
      SyncBlockInfo Map(BlockTable block);
      BlockTable Map(BlockInfo blockInfo);
   }
}
