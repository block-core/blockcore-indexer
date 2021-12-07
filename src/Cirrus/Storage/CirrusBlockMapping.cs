using System;
using System.Linq;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;
using Cirrus.Storage.Mongo.Types;
using Cirrus.Storage.Types;
using NBitcoin;

namespace Cirrus.Storage
{
   public class CirrusBlockMapping : IMapMongoBlockToStorageBlock
   {
      public SyncBlockInfo Map(MapBlock block)
      {
         var derived = block as CirrusBlock;

         if (derived is null)
            throw new ArgumentException("Not a Cirrus block");

         return new CirrusSyncBlockInfo
         {
            BlockIndex = derived.BlockIndex,
            BlockSize = derived.BlockSize,
            BlockHash = derived.BlockHash,
            BlockTime = derived.BlockTime,
            NextBlockHash = derived.NextBlockHash,
            PreviousBlockHash = derived.PreviousBlockHash,
            TransactionCount = derived.TransactionCount,
            Nonce = derived.Nonce,
            ChainWork = derived.ChainWork,
            Difficulty = derived.Difficulty,
            Merkleroot = derived.Merkleroot,
            PosModifierv2 = derived.PosModifierv2,
            PosHashProof = derived.PosHashProof,
            PosFlags = derived.PosFlags,
            PosChainTrust = derived.PosChainTrust,
            PosBlockTrust = derived.PosBlockTrust,
            PosBlockSignature = derived.PosBlockSignature,
            Confirmations = derived.Confirmations,
            Bits = derived.Bits,
            Version = derived.Version,
            SyncComplete = derived.SyncComplete,
            Bloom = derived.Bloom,
            ReceiptRoot = new uint256(derived.ReceiptRoot),
            HashStateRoot = new uint256(derived.HashStateRoot)
         };
      }

      public MapBlock Map(BlockInfo blockInfo)
      {
         var block = blockInfo as CirrusBlockInfo;

         if (block is null)
            throw new ArgumentException("Not a Cirrus block");

         return new CirrusBlock
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
            SyncComplete = false,
            Bloom = block.Bloom,
            ReceiptRoot = block.ReceiptRoot,
            HashStateRoot = block.HashStateRoot
         };
      }
   }
}
