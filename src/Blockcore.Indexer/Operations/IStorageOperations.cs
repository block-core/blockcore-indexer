namespace Blockcore.Indexer.Operations
{
   #region Using Directives

   using Blockcore.Indexer.Operations.Types;

   #endregion Using Directives

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

      void PushStorageBatch(StorageBatch storageBatch);
   }
}
