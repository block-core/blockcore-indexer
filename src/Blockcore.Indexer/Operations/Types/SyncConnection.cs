using System.Collections.Generic;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Indexer.Operations.Types
{
   using System;
   using Blockcore.Indexer.Settings;
   using Microsoft.Extensions.Options;

   /// <summary>
   /// Represents a minimal 
   /// </summary>
   public class NetworkConfig : Network
   {
      public NetworkConfig(IndexerSettings config, ChainSettings chainConfig, NetworkSettings networkConfig)
      {
         CoinTicker = chainConfig.Symbol;

         var consensusFactory = (ConsensusFactory)Activator.CreateInstance(Type.GetType(networkConfig.NetworkConsensusFactoryType));

         Consensus = new ConsensusConfig(config, consensusFactory);

         Base58Prefixes = new byte[12][];
         Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (networkConfig.NetworkPubkeyAddressPrefix) };
         Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (networkConfig.NetworkScriptAddressPrefix) };

         Bech32Encoders = new Bech32Encoder[2];
         var encoder = new Bech32Encoder(networkConfig.NetworkWitnessPrefix);
         Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
         Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

         // TODO
         //StandardScripts.RegisterStandardScriptTemplate(ColdStakingScriptTemplate);
      }
   }

   public class ConsensusConfig : Consensus
   {
      public ConsensusConfig(IndexerSettings config, ConsensusFactory consensusFactory) : base(
          consensusFactory: consensusFactory,
          consensusOptions: null,
          coinType: 0,
          hashGenesisBlock: uint256.Zero,
          subsidyHalvingInterval: 0,
          majorityEnforceBlockUpgrade: 0,
          majorityRejectBlockOutdated: 0,
          majorityWindow: 0,
          buriedDeployments: null,
          bip9Deployments: null,
          bip34Hash: uint256.Zero,
          ruleChangeActivationThreshold: 0,
          minerConfirmationWindow: 0,
          maxReorgLength: 0,
          defaultAssumeValid: uint256.Zero,
          maxMoney: 0,
          coinbaseMaturity: 0,
          premineHeight: 0,
          premineReward: 0,
          proofOfWorkReward: 0,
          powTargetTimespan: TimeSpan.Zero,
          powTargetSpacing: TimeSpan.Zero,
          powAllowMinDifficultyBlocks: false,
          posNoRetargeting: false,
          powNoRetargeting: false,
          powLimit: new Target(uint256.Zero),
          minimumChainWork: null,
          isProofOfStake: consensusFactory is PosConsensusFactory,
          lastPowBlock: 0,
          proofOfStakeLimit: null,
          proofOfStakeLimitV2: null,
          proofOfStakeReward: 0
      )
      {
      }
   }

   [Serializable]
   public class SyncConnection
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="SyncConnection"/> class.
      /// </summary>
      public SyncConnection(IOptions<IndexerSettings> config, IOptions<ChainSettings> chainConfig, IOptions<NetworkSettings> networkConfig)
      {
         IndexerSettings configuration = config.Value;
         ChainSettings chainConfiguration = chainConfig.Value;
         NetworkSettings networkConfiguration = networkConfig.Value;

         Symbol = chainConfiguration.Symbol;
         Password = configuration.RpcPassword;

         // Take the RPC Port from the Indexer configuration, if it is specified. Otherwise we'll use the default for this chain.
         RpcAccessPort = configuration.RpcAccessPort != 0 ? configuration.RpcAccessPort : networkConfiguration.RPCPort;

         ServerDomain = configuration.RpcDomain.Replace("{Symbol}", chainConfiguration.Symbol.ToLower());
         User = configuration.RpcUser;
         Secure = configuration.RpcSecure;
         StartBlockIndex = configuration.StartBlockIndex;

         // This can be replaced with a specific the network class of a specific coin
         // Or use the config values to simulate the network class.
         Network = new NetworkConfig(configuration, chainConfiguration, networkConfiguration);

         RecentItems = new Buffer<(DateTime Inserted, TimeSpan Duration, long Size)>(5000);
      }

      public NBitcoin.Network Network { get; }

      public string Symbol { get; set; }

      public string Password { get; set; }

      public int RpcAccessPort { get; set; }

      public bool Secure { get; set; }

      public string ServerDomain { get; set; }

      public string ServerIp { get; set; }

      public string ServerName { get; set; }

      public string User { get; set; }

      public long StartBlockIndex { get; set; }

      public Buffer<(DateTime Inserted, TimeSpan Duration, long Size)> RecentItems { get; set; }
   }

   public class Buffer<T> : Queue<T>
   {
      private int? maxCapacity { get; set; }

      public Buffer() { maxCapacity = null; }
      public Buffer(int capacity) { maxCapacity = capacity; }

      public void Add(T newElement)
      {
         if (Count == (maxCapacity ?? -1)) Dequeue(); // no limit if maxCapacity = null
         Enqueue(newElement);
      }
   }
}
