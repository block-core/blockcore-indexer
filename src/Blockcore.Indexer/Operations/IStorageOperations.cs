namespace Blockcore.Indexer.Operations
{
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage.Types;

   /// <summary>
   /// The StorageOperations interface.
   /// </summary>
   public interface IStorageOperations
   {
      /// <summary>
      /// Validate a block.
      /// </summary>
      void ValidateBlock(SyncBlockTransactionsOperation item);

      /// <summary>
      /// Insert transactions.
      /// </summary>
      InsertStats InsertTransactions(SyncBlockTransactionsOperation item);

      void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item);

      SyncBlockInfo PushStorageBatch(StorageBatch storageBatch);
   }
}
