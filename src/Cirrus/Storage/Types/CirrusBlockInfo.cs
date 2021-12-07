using Blockcore.Indexer.Client.Types;

namespace Cirrus.Storage.Types
{
   public class CirrusBlockInfo : BlockInfo
   {
      public byte[] HashStateRoot { get; set; }
      public byte[] ReceiptRoot{ get; set; }
      public byte[] Bloom { get; set; }
   }
}
