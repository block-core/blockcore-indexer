using Blockcore.Indexer.Client;
using Blockcore.Indexer.Operations.Types;

namespace Cirrus.Client
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
