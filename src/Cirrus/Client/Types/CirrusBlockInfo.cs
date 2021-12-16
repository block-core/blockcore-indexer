using Blockcore.Indexer.Client.Types;
using Newtonsoft.Json;

namespace Cirrus.Client.Types
{
   public class CirrusBlockInfo : BlockInfo
   {

      public byte[] HashStateRoot { get; set; }

      public byte[] ReceiptRoot{ get; set; }

      public byte[] Bloom { get; set; }
   }
}
