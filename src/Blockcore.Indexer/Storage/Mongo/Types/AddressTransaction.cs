namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class AddressTransaction
   {
      public long UniquId { get; set; }
      public string Address { get; set; }
      public int AddressHash { get; set; }
      public string TransactionId { get; set; }
      public int AddressCounter { get; set; }
      public long BlockIndex { get; set; }
   }
}
