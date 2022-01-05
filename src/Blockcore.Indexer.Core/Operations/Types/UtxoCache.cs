using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;

namespace Blockcore.Indexer.Core.Operations.Types
{
   public class UtxoCacheItem
   {
      public string Address { get; set; }
      public long Value { get; set; }
   }

   public class UtxoCache : IUtxoCache
   {
      private readonly ILogger<UtxoCache> logger;
      private readonly ConcurrentDictionary<string, UtxoCacheItem> cache;

      private readonly int maxItemInCache = 0;

      public UtxoCache(ILogger<UtxoCache> logger)
      {
         this.logger = logger;
         cache = new ConcurrentDictionary<string, UtxoCacheItem>();
      }

      public int CacheSize { get { return cache.Count; } }

      public UtxoCacheItem GetOne(string outpoint)
      {
         if (cache.TryGetValue(outpoint, out UtxoCacheItem utxo))
         {
            return new UtxoCacheItem { Value = utxo.Value, Address = utxo.Address };
         }

         return null;
      }

      public void AddToCache(IEnumerable<OutputTable> outputs)
      {
         int maxToAdd = maxItemInCache - cache.Count;
         foreach (OutputTable output in outputs.Take(maxToAdd))
         {
            cache.TryAdd($"{output.Outpoint.TransactionId}-{output.Outpoint.OutputIndex}", new UtxoCacheItem { Value = output.Value, Address = output.Address });
         }
      }

      public void RemoveFromCache(IEnumerable<InputTable> inputs)
      {
         foreach (InputTable output in inputs)
         {
            cache.TryRemove($"{output.Outpoint.TransactionId}-{output.Outpoint.OutputIndex}", out _);
         }
      }
   }
}
