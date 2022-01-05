using Blockcore.Features.PoA;
using NBitcoin;

namespace Blockcore.Indexer.Cirrus
{
   public class SmartContractPoABlockHeader : PoABlockHeader
   {
      /// <summary>
      /// Root of the state trie after execution of this block.
      /// </summary>
      private uint256 hashStateRoot;
      public uint256 HashStateRoot { get { return hashStateRoot; } set { hashStateRoot = value; } }

      /// <summary>
      /// Root of the receipt trie after execution of this block.
      /// </summary>
      private uint256 receiptRoot;
      public uint256 ReceiptRoot { get { return receiptRoot; } set { receiptRoot = value; } }

      /// <summary>
      /// Bitwise-OR of all the blooms generated from all of the smart contract transactions in the block.
      /// </summary>
      private Bloom logsBloom;
      public Bloom LogsBloom { get { return logsBloom; } set { logsBloom = value; } }

      public SmartContractPoABlockHeader() : base()
      {
         hashStateRoot = 0;
         receiptRoot = 0;
         logsBloom = new Bloom();
      }

      public override void ReadWrite(BitcoinStream stream)
      {
         base.ReadWrite(stream);
         stream.ReadWrite(ref hashStateRoot);
         stream.ReadWrite(ref receiptRoot);
         stream.ReadWrite(ref logsBloom);
      }

      /// <inheritdoc />
      protected override void ReadWriteHashingStream(BitcoinStream stream)
      {
         base.ReadWriteHashingStream(stream);

         // All fields included in SC header
         stream.ReadWrite(ref hashStateRoot);
         stream.ReadWrite(ref receiptRoot);
         stream.ReadWrite(ref logsBloom);
      }
   }
}
