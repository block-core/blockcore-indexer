namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class MapTransaction
   {
      public byte[] RawTransaction { get; set; }

      public string TransactionId { get; set; }
   }
}
