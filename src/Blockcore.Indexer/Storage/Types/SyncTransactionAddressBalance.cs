namespace Blockcore.Indexer.Storage.Types
{
   #region Using Directives

   using System.Collections.Generic;

   #endregion

   public class SyncTransactionAddressBalance
   {
      #region Public Properties

      public long Available { get; set; }

      public long? Received { get; set; }

      public long? Sent { get; set; }

      public long Unconfirmed { get; set; }

      public IEnumerable<SyncTransactionAddressItem> Items { get; set; }

      #endregion
   }
}
