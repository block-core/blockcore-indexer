using System.Collections.Generic;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Core.Operations
{
   /// <summary>
   /// Maintain a cache of unspent outputs
   /// </summary>
   public interface IUtxoCache
   {
      int CacheSize { get; }

      UtxoCacheItem GetOne(string outpoint);
      void AddToCache(IEnumerable<OutputTable> outputs);

      void RemoveFromCache(IEnumerable<InputTable> inputs);
   }
}
