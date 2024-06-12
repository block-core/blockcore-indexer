using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Operations
{
   /// <summary>
   /// The StorageOperations interface.
   /// </summary>
   public interface IStorageOperations
   {

      void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item);

      SyncBlockInfo PushStorageBatch(StorageBatch storageBatch);

      void InsertMempoolTransactions(SyncBlockTransactionsOperation item);
   }
}
