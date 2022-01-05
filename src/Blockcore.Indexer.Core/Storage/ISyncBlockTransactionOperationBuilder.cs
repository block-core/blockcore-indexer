using Blockcore.Consensus.BlockInfo;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Storage
{
   public interface ISyncBlockTransactionOperationBuilder
   {
      SyncBlockTransactionsOperation BuildFromClientData(BlockInfo blockInfo, Block block);
   }
}
