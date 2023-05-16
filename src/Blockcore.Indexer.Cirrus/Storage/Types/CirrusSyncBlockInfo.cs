using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.NBitcoin;

namespace Blockcore.Indexer.Cirrus.Storage.Types
{
   public class CirrusSyncBlockInfo : SyncBlockInfo
   {
      public uint256 HashStateRoot { get; set; }
      public uint256 ReceiptRoot{ get; set; }
      public Bloom Bloom { get; set; }
   }
}
