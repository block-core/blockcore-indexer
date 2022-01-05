namespace Blockcore.Indexer.Core.Storage.Mongo.Types
{
   public class AddressComputedTable
   {
      public string Id { get; set; }

      public string Address { get; set; }

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

      public long CountUtxo { get; set; }
   }
}
