using Blockcore.Consensus.BlockInfo;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Storage
{
   public class SyncBlockTransactionOperationBuilder : ISyncBlockTransactionOperationBuilder
   {

      public SyncBlockTransactionsOperation BuildFromClientData(BlockInfo blockInfo, Block block)
      {
         return new SyncBlockTransactionsOperation { BlockInfo = blockInfo, Transactions = block.Transactions };
      }
   }
}
