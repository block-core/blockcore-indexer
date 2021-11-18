using System;
using System.Collections.Generic;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Extensions;
using Blockcore.Indexer.Storage.Types;

namespace Blockcore.Indexer.Operations.Types
{
   public class SyncingBlocks
   {
      public SyncBlockInfo StoreTip { get; set; }

      public BlockInfo PullingTip { get; set; }

      public bool ReorgMode { get; set; }

      public bool IndexMode { get; set; }

      public long ChainTipHeight { get; set; }

      public bool Blocked { get; set; }

      public List<string> LocalMempoolView { get; set; } = new List<string>();

      public bool IbdMode()
      {
         DateTime tipTime = UnixUtils.UnixTimestampToDate(StoreTip.BlockTime);
         if ((DateTime.UtcNow - tipTime).TotalHours > 2)
         {
            return true;
         }

         return false;
      }
   }
}
