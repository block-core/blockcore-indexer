using System;
using System.Collections.Generic;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Networks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Blockcore.Indexer.Core.Models
{
   public class CoinInfo
   {
      public long BlockHeight { get; set; }

      public string Name { get; set; }

      public string Symbol { get; set; }

      public string Description { get; set; }

      public string Url { get; set; }

      public string Logo { get; set; }

      public string Icon { get; set; }

      /// <summary>
      /// Returns information and statistics returned directly from the node.
      /// </summary>
      public Statistics Node { get; set; }

      /// <summary>
      /// Returns the known network configuration. This information is retrieved from the network configuration, not from the node.
      /// </summary>
      public NetworkInfo Configuration { get; set; }
   }

   public class NetworkInfo
   {
      public int DefaultPort { get; set; }

      public int DefaultRPCPort { get; set; }

      public int DefaultAPIPort { get; set; }

      public int DefaultSignalRPort { get; set; }

      public long FallbackFee { get; set; }

      public long MinRelayTxFee { get; set; }

      public long MinTxFee { get; set; }

      [JsonConverter(typeof(StringEnumConverter))]
      public NetworkType NetworkType { get; set; }

      public string Name { get; set; }

      public List<string> SeedNodes { get; set; }

      public List<string> DNSSeeds { get; set; }

      public int DefaultMaxInboundConnections { get; set; }

      public int DefaultMaxOutboundConnections { get; set; }

      public DateTime GenesisDate { get; set; }

      public string GenesisHash { get; set; }

      public ConsensusInfo Consensus { get; set; }
   }

   public class ConsensusInfo {
      public int CoinType { get; set; }

      public long CoinbaseMaturity { get; set; }

      public uint MaxReorgLength { get; set; }

      public bool IsProofOfStake { get; set; }

      public long MaxMoney { get; set; }

      public int LastPOWBlock { get; set; }

      public decimal PremineReward { get; set; }

      public decimal ProofOfStakeReward { get; set; }

      public decimal ProofOfWorkReward { get; set; }

      public TimeSpan TargetSpacing { get; set; }
   }

   public class FeeEstimation
   {
      public int Confirmations { get; set; }
      public long FeeRateet { get; set; }
   }

   public class FeeEstimations
   {
      public List<FeeEstimation>  Fees { get; set; }
   }

   public class Statistics
   {
      /// <summary>
      /// Gets or sets the coin tag.
      /// </summary>
      public string Symbol { get; set; }

      /// <summary>
      /// Gets or sets the sync progress.
      /// </summary>
      public string Progress { get; set; }

      /// <summary>
      /// How many blocks are let to sync (doest no include indexing time)
      /// </summary>
      public long BlocksLeftToSync { get; set; }

      /// <summary>
      /// Gets or sets the number of transactions in pool.
      /// </summary>
      public int TransactionsInPool { get; set; }

      /// <summary>
      /// Gets or sets the current block index.
      /// </summary>
      public long SyncBlockIndex { get; set; }

      public BlockchainInfoModel Blockchain { get; set; }

      public NetworkInfoModel Network { get; set; }

      public int BlocksPerMinute { get; set; }

      public double AvgBlockPersistInSeconds { get; set; }

      public double AvgBlockSizeKb { get; set; }

      public string Error { get; set; }

      public bool IsInIBDMode { get; set; }

   }
}
