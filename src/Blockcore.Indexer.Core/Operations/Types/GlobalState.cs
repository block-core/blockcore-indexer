using System;
using System.Collections.Concurrent;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Operations.Types
{
   /// <summary>
   /// Represent a the state of the node.
   /// </summary>
   public class GlobalState
   {
      /// <summary>
      /// The last block that is persisted to disk and is in completed mode.
      /// Blocks and block meta data that where just discovered can be in disk
      /// in an incomplete state for a few moment while the block is being persisted
      /// </summary>
      public SyncBlockInfo StoreTip { get; set; }

      /// <summary>
      /// The last block that was fetch from the fullnode client.
      /// </summary>
      public BlockInfo PullingTip { get; set; }

      /// <summary>
      /// Indicate the node is in reorganization of the blockchain.
      /// This can heppen when a current chain is not the main chain anymore
      /// and blocks and block meta data need to be deleted.
      /// </summary>
      public bool ReorgMode { get; set; }

      /// <summary>
      /// Indicate that indexes are running on the db
      /// </summary>
      public bool IndexMode { get; set; }

      /// <summary>
      /// Indicate that indexes completed running
      /// </summary>
      public bool IndexModeCompleted { get; set; }

      /// <summary>
      /// The tip of the chain as the fullnode reports it.
      /// The indexer and fullnode tip may be different while the
      /// indexer is in sync mode.
      /// </summary>
      public long ChainTipHeight { get; set; }

      /// <summary>
      /// Block runneres form running
      /// todo: consider deleting this unused property
      /// </summary>
      public bool Blocked { get; set; }

      /// <summary>
      /// This represents an a memory view of all mempool transactions in store
      /// </summary>
      public ConcurrentDictionary<string, string> LocalMempoolView { get; set; } = new ();

      /// <summary>
      /// Indicates is the last persisted tip is in initial block download mode.
      /// Normally if a block is 2 hour behind our current time it is considered in ibd.
      /// todo: make the ibd 2 hour slot configurable
      /// </summary>
      public bool IbdMode()
      {
         var storeTip = StoreTip;

         if (storeTip == null)
            return true;

         DateTime tipTime = UnixUtils.UnixTimestampToDate(storeTip.BlockTime);
         if ((DateTime.UtcNow - tipTime).TotalHours > 2)
         {
            return true;
         }

         return false;
      }
   }
}
