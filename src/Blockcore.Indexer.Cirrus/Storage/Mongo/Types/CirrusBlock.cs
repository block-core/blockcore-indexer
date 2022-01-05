using Blockcore.Indexer.Core.Storage.Mongo.Types;
using NBitcoin;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types
{
   public class CirrusBlock : BlockTable
   {
      public byte[] HashStateRoot { get; set; }
      public byte[] ReceiptRoot{ get; set; }
      public byte[] Bloom { get; set; }
   }
}
