namespace Blockcore.Indexer.Storage.Mongo.Types
{
   using System.Collections.Generic;

   public class MapTransactionAddressHistoryComputed
   {
      public string Id { get; set; }

      public List<string> Addresses { get; set; }

      public string EntryType { get; set; }

      public long AmountSent { get; set; }

      public long AmountReceived { get; set; }

      public string TransactionId { get; set; }

      public long Position { get; set; }

      public long BlockIndex { get; set; }
   }
}
