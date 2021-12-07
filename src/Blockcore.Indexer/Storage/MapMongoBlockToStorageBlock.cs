using System.Linq;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;

namespace Blockcore.Indexer.Storage
{
   public class MapMongoBlockToStorageBlock : IMapMongoBlockToStorageBlock
   {
      public SyncBlockInfo Map(MapBlock block) => new SyncBlockInfo
      {
         BlockIndex = block.BlockIndex,
         BlockSize = block.BlockSize,
         BlockHash = block.BlockHash,
         BlockTime = block.BlockTime,
         NextBlockHash = block.NextBlockHash,
         PreviousBlockHash = block.PreviousBlockHash,
         TransactionCount = block.TransactionCount,
         Nonce = block.Nonce,
         ChainWork = block.ChainWork,
         Difficulty = block.Difficulty,
         Merkleroot = block.Merkleroot,
         PosModifierv2 = block.PosModifierv2,
         PosHashProof = block.PosHashProof,
         PosFlags = block.PosFlags,
         PosChainTrust = block.PosChainTrust,
         PosBlockTrust = block.PosBlockTrust,
         PosBlockSignature = block.PosBlockSignature,
         Confirmations = block.Confirmations,
         Bits = block.Bits,
         Version = block.Version,
         SyncComplete = block.SyncComplete
      };


      public MapBlock Map(BlockInfo block) =>
         new MapBlock
         {
            BlockIndex = block.Height,
            BlockHash = block.Hash,
            BlockSize = block.Size,
            BlockTime = block.Time,
            NextBlockHash = block.NextBlockHash,
            PreviousBlockHash = block.PreviousBlockHash,
            TransactionCount = block.Transactions.Count(),
            Bits = block.Bits,
            Confirmations = block.Confirmations,
            Merkleroot = block.Merkleroot,
            Nonce = block.Nonce,
            ChainWork = block.ChainWork,
            Difficulty = block.Difficulty,
            PosBlockSignature = block.PosBlockSignature,
            PosBlockTrust = block.PosBlockTrust,
            PosChainTrust = block.PosChainTrust,
            PosFlags = block.PosFlags,
            PosHashProof = block.PosHashProof,
            PosModifierv2 = block.PosModifierv2,
            Version = block.Version,
            SyncComplete = false
         };
   }
}
