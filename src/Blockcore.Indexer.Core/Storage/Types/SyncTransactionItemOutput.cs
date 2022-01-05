namespace Blockcore.Indexer.Core.Storage.Types
{
   public class SyncTransactionItemOutput
   {
      public int Index { get; set; }

      public string Address { get; set; }

      public string OutputType { get; set; }

      public long Value { get; set; }

      public string ScriptPubKey { get; set; }

      public string SpentInTransaction { get; set; }
   }
}
