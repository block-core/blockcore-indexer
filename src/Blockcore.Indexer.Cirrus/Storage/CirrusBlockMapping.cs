using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Cirrus.Storage.Types;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.NBitcoin;

namespace Blockcore.Indexer.Cirrus.Storage
{
   public class CirrusBlockMapping : IMapMongoBlockToStorageBlock
   {
      public SyncBlockInfo Map(BlockTable block)
      {
         if (block is not CirrusBlock derived)
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
            Bloom = derived.Bloom == null ? new Bloom() : new Bloom(derived.Bloom),
            ReceiptRoot = derived.ReceiptRoot == null ? null : new uint256(derived.ReceiptRoot),
            HashStateRoot = derived.HashStateRoot == null ? null : new uint256(derived.HashStateRoot)
         };
      }

      public BlockTable Map(BlockInfo blockInfo)
      {
         if (blockInfo is not CirrusBlockInfo block)
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
            Bloom = block.Bloom ?? null,
            ReceiptRoot = block.ReceiptRoot ?? null,
            HashStateRoot = block.HashStateRoot ?? null
         };
      }
   }
}
