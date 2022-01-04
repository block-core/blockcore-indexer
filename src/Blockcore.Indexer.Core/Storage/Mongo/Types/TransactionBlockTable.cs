namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class TransactionBlockTable
   {
      public long BlockIndex { get; set; }

      public string TransactionId { get; set; }
   }
}
