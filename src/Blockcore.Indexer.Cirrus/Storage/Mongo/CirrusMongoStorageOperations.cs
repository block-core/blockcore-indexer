using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class CirrusMongoStorageOperations : MongoStorageOperations
   {
      public CirrusMongoStorageOperations(
         SyncConnection syncConnection,
         IStorage storage,
         IUtxoCache utxoCache,
         IOptions<IndexerSettings> configuration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         IScriptInterpeter scriptInterpeter):
         base(
             syncConnection,
             storage,
             utxoCache,
             configuration,
             globalState,
             mongoBlockToStorageBlock,
             scriptInterpeter)
      {
      }

      protected override void OnAddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {

      }

      protected override void OnPushStorageBatch(StorageBatch storageBatch)
      {

      }
   }
}
