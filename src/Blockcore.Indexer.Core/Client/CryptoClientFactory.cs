using Blockcore.Indexer.Core.Operations.Types;
using Microsoft.Extensions.Caching.Memory;

namespace Blockcore.Indexer.Core.Client
{
   #region Using Directives

   // using System.Runtime.Caching;

   #endregion Using Directives

   /// <summary>
   ///  a factory to create clients.
   /// </summary>
   public class CryptoClientFactory : ICryptoClientFactory
   {
      /// <summary>
      ///     Defines a cache object to hold storage sources.
      /// </summary>
      private readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

      public IBlockchainClient Create(SyncConnection connection)
      {
         return GetOrCreateBitcoinClient(connection.ServerDomain, connection.RpcAccessPort, connection.User,
            connection.Password, connection.Secure);
      }

      BitcoinClient GetOrCreateBitcoinClient(string connection, int port, string user, string encPass,
         bool secure)
      {
         lock (Cache)
         {
            // Set cache key name
            string cacheKey = string.Format("{0}:{1}:{2}:{3}", connection, port, user, secure);

            if (Cache.TryGetValue(cacheKey, out BitcoinClient client))
               return client;

            client = BitcoinClient.Create(connection, port, user, encPass, secure);

            Cache.Set(cacheKey, client);

            return client;
            //return Cache.GetOrCreate(cacheKey, t => BitcoinClient.Create(connection, port, user, encPass, secure));
         }
      }
   }
}
