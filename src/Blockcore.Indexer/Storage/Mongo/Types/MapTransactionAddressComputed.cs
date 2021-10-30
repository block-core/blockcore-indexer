namespace Blockcore.Indexer.Storage.Mongo.Types
{
   using System.Collections.Generic;

   public class MapTransactionAddressComputed
   {
      public string Id { get; set; }

      public List<string> Addresses { get; set; }

      public long Available { get; set; }

      public long Received { get; set; }

      public long Sent { get; set; }

      public long Staked { get; set; }

      public long Mined { get; set; }

      public long ComputedBlockIndex { get; set; }

      public long TotalReceived { get; set; }

      public long TotalSent { get; set; }

      public long TotalStaked { get; set; }

      public long TotalMined { get; set; }

      public long TotalSpendableTransactions { get; set; }
   }
}
