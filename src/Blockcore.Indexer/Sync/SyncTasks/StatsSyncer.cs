using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Api.Handlers;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Extensions;
using Blockcore.Indexer.Operations;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Sync.SyncTasks
{
   /// <summary>
   /// 
   /// </summary>
   public class StatsSyncer : TaskRunner
   {
      private readonly ILogger<StatsSyncer> log;

      private readonly StatsHandler statsHandler;

      private readonly IStorageOperations storageOperations;

      private readonly MongoData data;

      private readonly System.Diagnostics.Stopwatch watch;

      /// <summary>
      /// 
      /// </summary>
      public StatsSyncer(
         IOptions<IndexerSettings> configuration,
         ISyncOperations syncOperations,
         IStorageOperations storageOperations,
         IStorage storage,
         SyncConnection syncConnection,
         StatsHandler statsHandler,
         ILogger<StatsSyncer> logger) : base(configuration, logger)
      {
         log = logger;

         data = (MongoData)storage;
         this.statsHandler = statsHandler;
         this.storageOperations = storageOperations;

         // Only run the StatsSyncer every 5 minute.
         Delay = TimeSpan.FromMinutes(5);
         watch = Stopwatch.Start();
      }

      public override async Task<bool> OnExecute()
      {
         watch.Restart();

         List<PeerInfo> peers = await statsHandler.Peers();

         watch.Stop();

         log.LogDebug($"Time taken to retrieve peers from node: {watch.Elapsed.TotalSeconds}. Processing {peers.Count} peers and updating database.");

         watch.Restart();

         // TODO: Look into potential optimization on updates, not replacing the whole document for all connected nodes all the time.
         foreach (PeerInfo peer in peers)
         {
            await data.InsertPeer(peer);
         }

         log.LogDebug($"Time taken to update peers in database: {watch.Elapsed.TotalSeconds}.");

         return await Task.FromResult(false);
      }
   }
}
