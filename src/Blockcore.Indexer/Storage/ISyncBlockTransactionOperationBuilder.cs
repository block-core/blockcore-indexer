using Blockcore.Consensus.BlockInfo;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Operations.Types;

namespace Blockcore.Indexer.Storage
{
   public interface ISyncBlockTransactionOperationBuilder
   {
      SyncBlockTransactionsOperation BuildFromClientData(BlockInfo blockInfo, Block block);
   }
}
