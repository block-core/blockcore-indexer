using Blockcore.Indexer.Operations.Types;

namespace Blockcore.Indexer.Client
{
   public interface ICryptoClientFactory
   {
      BitcoinClient Create(string connection, int port, string user, string encPass, bool secure);

      BitcoinClient Create(SyncConnection connection);
   }
}
