using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;

namespace Blockcore.Indexer.Storage
{
   public interface IMapMongoBlockToStorageBlock
   {
      SyncBlockInfo Map(BlockTable block);
      BlockTable Map(BlockInfo blockInfo);
   }
}
