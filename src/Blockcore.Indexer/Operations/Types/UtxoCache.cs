using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Operations.Types
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

      private readonly int maxItemInCache = 30_000_000;

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

      public void AddToCache(IEnumerable<AddressForOutput> outputs)
      {
         int maxToAdd = maxItemInCache - cache.Count;
         foreach (AddressForOutput output in outputs.Take(maxToAdd))
         {
            cache.TryAdd(output.Outpoint, new UtxoCacheItem { Value = output.Value, Address = output.Address });
         }
      }

      public void RemoveFromCache(IEnumerable<AddressForInput> inputs)
      {
         foreach (AddressForInput output in inputs)
         {
            cache.TryRemove(output.Outpoint, out _);
         }
      }
   }
}