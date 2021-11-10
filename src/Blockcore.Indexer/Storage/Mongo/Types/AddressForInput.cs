namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class AddressForInput
   {
      public long UniquID { get; set; }

      public string Address { get; set; }

      public string TransactionId { get; set; }

      public int OutputIndex { get; set; }
   }
}
