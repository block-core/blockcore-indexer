using System.Collections.Generic;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Storage.Mongo.Types;

namespace Blockcore.Indexer.Operations
{
   /// <summary>
   /// Maintain a cache of unspent outputs 
   /// </summary>
   public interface IUtxoCache
   {
      int CacheSize { get; }

      UtxoCacheItem GetOne(string outpoint);
      void AddToCache(IEnumerable<AddressForOutput> outputs);

      void RemoveFromCache(IEnumerable<AddressForInput> inputs);
   }
}
