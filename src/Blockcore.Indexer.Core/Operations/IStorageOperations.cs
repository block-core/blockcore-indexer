namespace Blockcore.Indexer.Operations
{
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage.Types;

   /// <summary>
   /// The StorageOperations interface.
   /// </summary>
   public interface IStorageOperations
   {

      void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item);

      SyncBlockInfo PushStorageBatch(StorageBatch storageBatch);

      InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item);
   }
}
