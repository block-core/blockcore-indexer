namespace Blockcore.Indexer.Storage.Types
{
   using System.Collections.Generic;

   public class SyncTransactionAddressBalance
   {
      public long Available { get; set; }

      public long? Received { get; set; }

      public long? Sent { get; set; }

      public long Unconfirmed { get; set; }

      public IEnumerable<SyncTransactionAddressItem> Items { get; set; }
   }
}
