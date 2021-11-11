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
   public class UtxoCache : IUtxoCache
   {
      private readonly IStorage storage;
      private readonly ILogger<UtxoCache> logger;
      private readonly ConcurrentDictionary<string, AddressForOutput> cache;

      private readonly int maxItemInCache = 10_000_000;

      public UtxoCache(IStorage storage, ILogger<UtxoCache> logger)
      {
         this.storage = storage;
         this.logger = logger;
         cache = new ConcurrentDictionary<string, AddressForOutput>();
      }

      public int CacheSize { get { return cache.Count; } }

      public AddressForOutput GetOrFetch(string outpoint, bool addToCache = false)
      {
         if (cache.TryGetValue(outpoint, out AddressForOutput utxo))
         {
            return utxo;
         }

         var data = (MongoData)storage;

         IMongoQueryable<AddressForOutput> query = data.AddressForOutput.AsQueryable()
            .Where(w => w.Outpoint == outpoint);

         AddressForOutput output = query.SingleOrDefault();

         if (output == null)
         {
            throw new ApplicationException("output not found");
         }

         if (addToCache)
            cache.TryAdd(outpoint, output);

         return output;
      }

      public void AddToCache(IEnumerable<AddressForOutput> outputs)
      {
         int maxToAdd = maxItemInCache - cache.Count;
         foreach (AddressForOutput output in outputs.Take(maxToAdd))
         {
            cache.TryAdd(output.Outpoint, output);
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
