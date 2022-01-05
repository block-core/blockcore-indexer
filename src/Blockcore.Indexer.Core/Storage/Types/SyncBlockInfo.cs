namespace Blockcore.Indexer.Core.Storage.Types
{
   public class SyncBlockInfo
   {
      public string BlockHash { get; set; }

      public long BlockIndex { get; set; }

      public long BlockSize { get; set; }

      public long BlockTime { get; set; }

      public string NextBlockHash { get; set; }

      public string PreviousBlockHash { get; set; }

      public string ReveresedBlockIndex { get; set; }

      public bool SyncComplete { get; set; }

      public int TransactionCount { get; set; }

      public string ETag { get; set; }

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
