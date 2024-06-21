using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blockcore.Indexer.Core.Client.Types
{
   public class PeerInfo
   {
      public string Addr { get; set; }

      public string AddrLocal { get; set; }

      public string Services { get; set; }

      public long LastSend { get; set; }

      public long LastRecv { get; set; }

      public long BytesSent { get; set; }

      public long BytesRecv { get; set; }

      public int ConnTime { get; set; }

      public double PingTime { get; set; }

      public double Version { get; set; }

      public string SubVer { get; set; }

      public bool Inbound { get; set; }

      public long StartingHeight { get; set; }

      public int BanScore { get; set; }

      [JsonProperty("synced_headers")]
      public long SyncedHeaders { get; set; }

      [JsonProperty("synced_blocks")]
      public long SyncedBlocks { get; set; }

      public IList<long> InFlight { get; set; }

      public bool WhiteListed { get; set; }

      public DateTime LastSeen { get; set; }
   }
}
