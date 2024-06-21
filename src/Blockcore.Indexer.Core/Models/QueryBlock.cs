
namespace Blockcore.Indexer.Core.Models
{
   public class QueryBlock
   {

      /// <summary>
      /// Gets or sets the Symbol.
      /// </summary>
      public string Symbol { get; set; }

      /// <summary>
      /// Gets or sets the block hash.
      /// </summary>
      public string BlockHash { get; set; }

      /// <summary>
      /// Gets or sets the block Height.
      /// </summary>
      public long BlockIndex { get; set; }

      /// <summary>
      /// Gets or sets the block Size.
      /// </summary>
      public long BlockSize { get; set; }

      /// <summary>
      /// Gets or sets the block Time.
      /// </summary>
      public long BlockTime { get; set; }

      /// <summary>
      /// Gets or sets the block NextHash.
      /// </summary>
      public string NextBlockHash { get; set; }

      /// <summary>
      /// Gets or sets the block PreviousHash.
      /// </summary>
      public string PreviousBlockHash { get; set; }

      /// <summary>
      /// Gets or sets a value indicating whether sync is complete for this block.
      /// </summary>
      public bool Synced { get; set; }

      /// <summary>
      /// Gets or sets the Transaction Count.
      /// </summary>
      public int TransactionCount { get; set; }

      public long Confirmations { get; set; }

      public string Bits { get; set; }

      public double Difficulty { get; set; }

      public string ChainWork { get; set; }

      public string Merkleroot { get; set; }

      public long Nonce { get; set; }

      public long Version { get; set; }

      public string PosBlockSignature { get; set; }

      public string PosModifierv2 { get; set; }

      public string PosFlags { get; set; }

      public string PosHashProof { get; set; }

      public string PosBlockTrust { get; set; }

      public string PosChainTrust { get; set; }
   }
}
