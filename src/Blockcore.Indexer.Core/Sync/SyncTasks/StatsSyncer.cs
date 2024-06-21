using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Handlers;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   /// <summary>
   ///
   /// </summary>
   public class StatsSyncer : TaskRunner
   {
      private readonly ILogger<StatsSyncer> log;

      private readonly StatsHandler statsHandler;

      private readonly IStorageOperations storageOperations;

      private readonly IStorage data;

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

         data = storage;
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
            await data.InsertPeer(new PeerDetails
            {
               Addr = peer.Addr,
               AddrLocal = peer.AddrLocal,
               Version = peer.Version,
               Inbound = peer.Inbound,
               Services = peer.Services,
               BanScore = peer.BanScore,
               BytesRecv = peer.BytesRecv,
               BytesSent = peer.BytesSent,
               ConnTime = peer.ConnTime,
               PingTime = peer.PingTime,
               InFlight = peer.InFlight,
               LastRecv = peer.LastRecv,
               LastSeen = peer.LastSeen,
               LastSend = peer.LastSend,
               StartingHeight = peer.StartingHeight,
               SubVer = peer.SubVer,
               SyncedBlocks = peer.SyncedBlocks,
               SyncedHeaders = peer.SyncedHeaders,
               WhiteListed = peer.WhiteListed
            });
         }

         log.LogInformation($"Time taken to update peers in database: {watch.Elapsed.TotalSeconds}.");

         return await Task.FromResult(false);
      }
   }
}
