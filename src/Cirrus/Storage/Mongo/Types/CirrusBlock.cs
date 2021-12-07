using Blockcore.Indexer.Storage.Mongo.Types;
using NBitcoin;

namespace Cirrus.Storage.Mongo.Types
{
   public class CirrusBlock : MapBlock
   {
      public byte[] HashStateRoot { get; set; }
      public byte[] ReceiptRoot{ get; set; }
      public byte[] Bloom { get; set; }
   }
}
