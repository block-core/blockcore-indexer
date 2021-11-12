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
      private readonly IStorage storage;
      private readonly ILogger<UtxoCache> logger;
      private readonly ConcurrentDictionary<string, UtxoCacheItem> cache;

      private readonly int maxItemInCache = 30_000_000;

      public UtxoCache(IStorage storage, ILogger<UtxoCache> logger)
      {
         this.storage = storage;
         this.logger = logger;
         cache = new ConcurrentDictionary<string, UtxoCacheItem>();
      }

      public int CacheSize { get { return cache.Count; } }

      public UtxoCacheItem GetOrFetch(string outpoint, bool addToCache = false)
      {
         if (cache.TryGetValue(outpoint, out UtxoCacheItem utxo))
         {
            return new UtxoCacheItem { Value = utxo.Value, Address = utxo.Address };
         }

         // todo: move this to the storage interface
         var data = (MongoData)storage;
         IMongoQueryable<AddressForOutput> query = data.AddressForOutput.AsQueryable()
            .Where(w => w.Outpoint == outpoint);
         AddressForOutput output = query.FirstOrDefault();

         if (output == null)
         {
            //throw new ApplicationException("output not found");
            return null;
         }

         var ret = new UtxoCacheItem {Value = output.Value, Address = output.Address};

         if (addToCache)
            cache.TryAdd(outpoint, ret);

         return ret;
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
