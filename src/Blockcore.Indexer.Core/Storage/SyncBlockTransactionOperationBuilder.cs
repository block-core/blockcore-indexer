using Blockcore.Consensus.BlockInfo;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Operations.Types;

namespace Blockcore.Indexer.Storage
{
   public class SyncBlockTransactionOperationBuilder : ISyncBlockTransactionOperationBuilder
   {

      public SyncBlockTransactionsOperation BuildFromClientData(BlockInfo blockInfo, Block block)
      {
         return new SyncBlockTransactionsOperation { BlockInfo = blockInfo, Transactions = block.Transactions };
      }
   }
}
