namespace Blockcore.Indexer.Core.Storage.Types
{
   public class SyncRawTransaction
   {
      public byte[] RawTransaction { get; set; }

      public string TransactionHash { get; set; }
   }
}
