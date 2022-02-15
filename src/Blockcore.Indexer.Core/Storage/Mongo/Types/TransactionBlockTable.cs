namespace Blockcore.Indexer.Core.Storage.Mongo.Types
{
   public class TransactionBlockTable
   {
      public uint BlockIndex { get; set; }

      public string TransactionId { get; set; }

      public int TransactionIndex { get; set; }
   }
}
