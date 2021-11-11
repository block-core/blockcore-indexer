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

namespace Blockcore.Indexer.Operations
{
   /// <summary>
   /// Maintain a cache of unspent outputs 
   /// </summary>
   public interface IUtxoCache
   {
      int CacheSize { get; }

      MapTransactionAddress GetOrFetch(string outpoint, bool addToCache = false);
      void AddToCache(IEnumerable<MapTransactionAddress> outputs);

      void RemoveFromCache(IEnumerable<MapTransactionAddress> outputs);
   }

   public class UtxoCache : IUtxoCache
   {
      private readonly IStorage storage;
      private readonly ILogger<UtxoCache> logger;
      private readonly ConcurrentDictionary<string, MapTransactionAddress> cache;

      private readonly int maxItemInCache = 1_000_000;

      public UtxoCache(IStorage storage, ILogger<UtxoCache> logger)
      {
         this.storage = storage;
         this.logger = logger;
         cache = new ConcurrentDictionary<string, MapTransactionAddress>();
      }

      public int CacheSize { get { return cache.Count; } }

      public MapTransactionAddress GetOrFetch(string outpoint, bool addToCache = false)
      {
         if (cache.TryGetValue(outpoint, out MapTransactionAddress utxo))
         {
            return utxo;
         }

         var data = (MongoData)storage;

         IMongoQueryable<MapTransactionAddress> query = data.MapTransactionAddress.AsQueryable()
            .Where(w => w.Id == outpoint);

         MapTransactionAddress output = IAsyncCursorSourceExtensions.SingleOrDefault(query);

         if (output == null)
         {
            throw new ApplicationException("output not found");
         }

         if (addToCache)
            cache.TryAdd(outpoint, output);

         return output;
      }

      public void AddToCache(IEnumerable<MapTransactionAddress> outputs)
      {
         int maxToAdd = maxItemInCache - cache.Count;
         foreach (MapTransactionAddress output in outputs.Take(maxToAdd))
         {
            cache.TryAdd(output.Id, output);
         }
      }

      public void RemoveFromCache(IEnumerable<MapTransactionAddress> outputs)
      {
         foreach (MapTransactionAddress output in outputs)
         {
            cache.TryRemove(output.Id, out _);
         }
      }
   }
}
