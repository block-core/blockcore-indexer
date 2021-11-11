using System.Collections.Generic;
using Blockcore.Indexer.Storage.Mongo.Types;

namespace Blockcore.Indexer.Operations
{
   /// <summary>
   /// Maintain a cache of unspent outputs 
   /// </summary>
   public interface IUtxoCache
   {
      int CacheSize { get; }

      AddressForOutput GetOrFetch(string outpoint, bool addToCache = false);
      void AddToCache(IEnumerable<AddressForOutput> outputs);

      void RemoveFromCache(IEnumerable<AddressForInput> inputs);
   }
}
