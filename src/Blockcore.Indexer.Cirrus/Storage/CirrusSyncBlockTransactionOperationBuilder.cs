using System;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Cirrus.Client.Types;

namespace Blockcore.Indexer.Cirrus.Storage
{
   public class CirrusSyncBlockTransactionOperationBuilder : ISyncBlockTransactionOperationBuilder
   {
      public SyncBlockTransactionsOperation BuildFromClientData(BlockInfo blockInfo, Block block)
      {
         var derivedBlockInfo = blockInfo as CirrusBlockInfo;
         var derivedBlockHeader = block.Header as SmartContractPoABlockHeader;
         if (derivedBlockInfo is null || derivedBlockHeader is null)
            throw new ArgumentException();

         derivedBlockInfo.Bloom = derivedBlockHeader.LogsBloom.ToBytes();
         derivedBlockInfo.ReceiptRoot = derivedBlockHeader.ReceiptRoot.ToBytes();
         derivedBlockInfo.HashStateRoot = derivedBlockHeader.HashStateRoot.ToBytes();

         return new SyncBlockTransactionsOperation { BlockInfo = derivedBlockInfo, Transactions = block.Transactions };
      }
   }
}
