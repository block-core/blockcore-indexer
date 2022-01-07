using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Cirrus.Client
{
   public class CirrusClientFactory : ICryptoClientFactory
   {
      public IBlockchainClient Create(SyncConnection connection)
      {
         return new CirrusClient(connection);
      }
   }
}
