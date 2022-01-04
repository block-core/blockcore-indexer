namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class Outpoint
   {
      public string TransactionId { get; set; }

      public int OutputIndex { get; set; }

      public override string ToString()
      {
         return TransactionId + "-" + OutputIndex;
      }
   }
}
