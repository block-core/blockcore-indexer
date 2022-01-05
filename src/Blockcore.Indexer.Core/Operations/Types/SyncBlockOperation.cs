using Blockcore.Indexer.Core.Client.Types;

namespace Blockcore.Indexer.Core.Operations.Types
{
   #region Using Directives

   #endregion

   /// <summary>
   /// The sync block info.
   /// </summary>
   public class SyncBlockOperation
   {
      /// <summary>
      /// Gets or sets the block info.
      /// </summary>
      public BlockInfo BlockInfo { get; set; }

      /// <summary>
      /// Gets or sets a value indicating whether incomplete block.
      /// </summary>
      public bool IncompleteBlock { get; set; }

      /// <summary>
      /// Gets or sets the last crypto block index.
      /// </summary>
      public long LastCryptoBlockIndex { get; set; }

      /// <summary>
      /// Gets or sets the transactions.
      /// </summary>
      public SyncPoolTransactions PoolTransactions { get; set; }
   }
}
