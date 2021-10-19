namespace Blockcore.Indexer.Operations.Types
{
   using System.Collections.Concurrent;
   using System.Collections.Generic;
   using Blockcore.Indexer.Client.Types;

   public class SyncingBlocks
   {
      public ConcurrentDictionary<string, BlockInfo> CurrentSyncing { get; set; }

      public BlockInfo LastBlock { get; set; }

      public Storage.Types.SyncBlockInfo StoreTip { get; set; }

      public BlockInfo PullingTip { get; set; }

      public bool ReorgMode { get; set; }

      public long LastClientBlockIndex { get; set; }

      public bool Blocked { get; set; }

      public List<string> CurrentPoolSyncing { get; set; }
   }
}
