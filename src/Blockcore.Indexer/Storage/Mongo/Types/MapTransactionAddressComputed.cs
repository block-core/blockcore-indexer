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

      public long CountReceived { get; set; }

      public long CountSent { get; set; }

      public long CountStaked { get; set; }

      public long CountMined { get; set; }

      public long TotalSpendableTransactions { get; set; }
   }
}
