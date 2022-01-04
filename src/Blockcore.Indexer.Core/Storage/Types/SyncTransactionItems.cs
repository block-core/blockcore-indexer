namespace Blockcore.Indexer.Storage.Types
{
   using System.Collections.Generic;

   public class SyncTransactionItems
   {
      public bool IsCoinbase { get; set; }

      public bool IsCoinstake { get; set; }

      public string LockTime { get; set; }

      public bool RBF { get; set; }

      public uint Version { get; set; }

      public List<SyncTransactionItemInput> Inputs { get; set; }

      public List<SyncTransactionItemOutput> Outputs { get; set; }
   }
}