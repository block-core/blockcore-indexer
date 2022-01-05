namespace Blockcore.Indexer.Core.Storage.Types
{
   public class AddressBalance
   {
      public string Address { get; set; }

      public long Available { get; set; }

      public long? Received { get; set; }

      public long? Sent { get; set; }

      public long Unconfirmed { get; set; }
   }
}
