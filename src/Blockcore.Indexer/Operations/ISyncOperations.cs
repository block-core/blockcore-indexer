namespace Blockcore.Indexer.Operations
{
   #region Using Directives

   using System.Threading.Tasks;
   using Blockcore.Indexer.Client.Types;
   using Blockcore.Indexer.Operations.Types;

   #endregion Using Directives

   /// <summary>
   /// The SyncOperations interface.
   /// </summary>
   public interface ISyncOperations
   {
      /// <summary>
      /// The sync block.
      /// </summary>
      SyncBlockOperation FindBlock(SyncConnection connection, SyncingBlocks container);

      /// <summary>
      /// The sync block.
      /// </summary>
      SyncPoolTransactions FindPoolTransactions(SyncConnection connection, SyncingBlocks container);

      /// <summary>
      /// The sync memory pool.
      /// </summary>
      SyncBlockTransactionsOperation SyncPool(SyncConnection connection, SyncPoolTransactions poolTransactions);

      /// <summary>
      /// The sync transactions.
      /// </summary>
      SyncBlockTransactionsOperation FetchFullBlock(SyncConnection connection, BlockInfo block);

      /// <summary>
      /// The check block reorganization.
      /// </summary>
      Task<Storage.Types.SyncBlockInfo> RewindToBestChain(SyncConnection connection);

      /// <summary>
      /// Delete all blocks that are not complete
      /// </summary>
      Storage.Types.SyncBlockInfo RewindToLastCompletedBlock();
   }
}
