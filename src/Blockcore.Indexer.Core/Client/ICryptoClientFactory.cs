using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Client
{
   public interface ICryptoClientFactory
   {
      IBlockchainClient Create(string connection, int port, string user, string encPass, bool secure);

      IBlockchainClient Create(SyncConnection connection);
   }
}
