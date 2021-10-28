namespace Blockcore.Indexer.Operations.Types
{
   using System.Collections.Concurrent;
   using System.Collections.Generic;
   using Blockcore.Indexer.Client.Types;

   public class SyncingBlocks
   {
      public Storage.Types.SyncBlockInfo StoreTip { get; set; }

      public BlockInfo PullingTip { get; set; }

      public bool ReorgMode { get; set; }

      public long ChainTipHeight { get; set; }

      public bool Blocked { get; set; }

      public List<string> LocalMempoolView { get; set; } = new List<string>();
   }
}
