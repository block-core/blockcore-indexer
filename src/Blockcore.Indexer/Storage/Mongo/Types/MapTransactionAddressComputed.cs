namespace Blockcore.Indexer.Storage.Mongo.Types
{
   using System.Collections.Generic;

   public class MapTransactionAddressComputed
   {
      public string Address { get; set; }

      public long Available { get; set; }

      public long? Received { get; set; }

      public long? Sent { get; set; }

      public long ComputedBlockIndex { get; set; }

      public long TotalTransactions { get; set; }

      public long TotalSpendableTransactions { get; set; }
   }
}
