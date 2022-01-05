namespace Blockcore.Indexer.Core.Storage.Mongo.Types
{
   public class TransactionTable
   {
      public byte[] RawTransaction { get; set; }

      public string TransactionId { get; set; }
   }
}
