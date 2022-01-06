using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Client
{
   public interface ICryptoClientFactory
   {
      IBlockchainClient Create(SyncConnection connection);
   }
}
