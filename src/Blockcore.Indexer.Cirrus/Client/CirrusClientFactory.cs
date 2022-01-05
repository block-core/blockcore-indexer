using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Cirrus.Client
{
   public class CirrusClientFactory : ICryptoClientFactory
   {
      public BitcoinClient Create(string connection, int port, string user, string encPass, bool secure)
      {
         return new CirrusClient(connection, port, user, encPass, secure);
      }

      public BitcoinClient Create(SyncConnection connection)
      {
         return new CirrusClient(connection.ServerDomain, connection.RpcAccessPort, connection.User,
            connection.Password, connection.Secure);
      }
   }
}
