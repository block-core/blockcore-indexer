namespace Blockcore.Indexer.Storage.Types
{
   public class SyncRawTransaction
   {
      public byte[] RawTransaction { get; set; }

      public string TransactionHash { get; set; }
   }
}