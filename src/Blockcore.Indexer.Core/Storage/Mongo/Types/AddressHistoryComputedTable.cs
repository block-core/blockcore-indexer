namespace Blockcore.Indexer.Core.Storage.Mongo.Types
{
   public class AddressHistoryComputedTable
   {
      public string Id { get; set; }
      public string Address { get; set; }

      public string EntryType { get; set; }

      public long AmountInInputs { get; set; }

      public long AmountInOutputs { get; set; }

      public string TransactionId { get; set; }

      public long Position { get; set; }

      public uint BlockIndex { get; set; }
   }
}
