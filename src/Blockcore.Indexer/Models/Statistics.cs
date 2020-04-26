namespace Blockcore.Indexer.Api.Handlers.Types
{
   using System;
   using System.Collections.Generic;
   using Blockcore.Indexer.Client.Types;
   using NBitcoin;
   using Newtonsoft.Json;
   using Newtonsoft.Json.Converters;

   public class CoinInfo
   {
      public long BlockHeight { get; set; }

      public string Name { get; set; }

      public string Symbol { get; set; }

      public string Description { get; set; }

      public string Url { get; set; }

      public string Logo { get; set; }

      public string Icon { get; set; }

      public NetworkInfo Network { get; set; }
   }

   public class NetworkInfo
   {
      public string CoinTicker { get; set; }

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

      public bool IsProofOfStake { get; set; }

      public long MaxMoney { get; set; }

      public int LastPOWBlock { get; set; }

      public decimal PremineReward { get; set; }

      public decimal ProofOfStakeReward { get; set; }

      public decimal ProofOfWorkReward { get; set; }

      public TimeSpan TargetSpacing { get; set; }
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
      /// Gets or sets the number of transactions in pool.
      /// </summary>
      public int TransactionsInPool { get; set; }

      /// <summary>
      /// Gets or sets the current block index.
      /// </summary>
      public long SyncBlockIndex { get; set; }

      public BlockchainInfoModel BlockchainInfo { get; set; }

      public NetworkInfoModel NetworkInfo { get; set; }

      public int BlocksPerMinute { get; set; }

      public double AvgBlockPersistInSeconds { get; set; }

      public double AvgBlockSizeKb { get; set; }

      public string Error { get; set; }

   }
}
