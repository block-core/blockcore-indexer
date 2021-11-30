namespace Blockcore.Indexer.Storage.Mongo.Types
{
   using System.Collections.Generic;

   public class AddressHistoryComputed
   {
      public string Id { get; set; }
      public string Address { get; set; }

      public string EntryType { get; set; }

      public long AmountInInputs { get; set; }

      public long AmountInOutputs { get; set; }

      public string TransactionId { get; set; }

      public long Position { get; set; }

      public long BlockIndex { get; set; }
   }
}
