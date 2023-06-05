using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Blockcore.Networks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Fluent;

namespace Blockcore.Indexer.Core.Handlers
{
   /// <summary>
   /// Handler to make get info about a blockchain.
   /// </summary>
   public class StatsHandler
   {
      private readonly SyncConnection syncConnection;

      private readonly IStorage storage;

      private readonly IndexerSettings configuration;

      private readonly ChainSettings chainConfiguration;

      private readonly NetworkSettings networkConfig;

      readonly ICryptoClientFactory clientFactory;
      readonly GlobalState globalState;

      private readonly ILogger<StatsHandler> log;


      readonly IMemoryCache cache;
      const string StatisticsKey = "Statistics";
      const string FeeEstimationKey = "FeeEstimation";

      /// <summary>
      /// Initializes a new instance of the <see cref="StatsHandler"/> class.
      /// </summary>
      public StatsHandler(
         SyncConnection connection,
         IStorage storage,
         IOptions<NetworkSettings> networkConfig,
         IOptions<IndexerSettings> configuration,
         IOptions<ChainSettings> chainConfiguration,
         ICryptoClientFactory clientFactory,
         GlobalState globalState,
         IMemoryCache cache,
         ILogger<StatsHandler> logger)
      {
         this.storage = storage;
         this.clientFactory = clientFactory;
         this.globalState = globalState;
         this.cache = cache;
         syncConnection = connection;
         this.configuration = configuration.Value;
         this.chainConfiguration = chainConfiguration.Value;
         this.networkConfig = networkConfig.Value;
         log = logger;
      }

      public async Task<StatsConnection> StatsConnection()
      {
         SyncConnection connection = syncConnection;
         IBlockchainClient client = clientFactory.Create(connection);

         int clientConnection = await client.GetConnectionCountAsync();
         return new StatsConnection { Connections = clientConnection };
      }

      public async Task<CoinInfo> CoinInformation()
      {
         long index = storage.GetLatestBlock()?.BlockIndex ?? 0;

         //SyncConnection connection = syncConnection;
         //BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         var coinInfo = new CoinInfo
         {
            BlockHeight = index,
            Name = chainConfiguration.Name,
            Symbol = chainConfiguration.Symbol,
            Description = chainConfiguration.Description,
            Url = chainConfiguration.Url,
            Logo = chainConfiguration.Logo,
            Icon = chainConfiguration.Icon
         };

         Statistics statitics = await Statistics();
         coinInfo.Node = statitics;

         // If we have network type available, we'll extend with extra metadata.
         if (syncConnection.HasNetworkType)
         {
            Network network = syncConnection.Network;
            IConsensus consensus = network.Consensus;

            coinInfo.Configuration = new NetworkInfo
            {
               DefaultAPIPort = network.DefaultAPIPort,
               DefaultMaxInboundConnections = network.DefaultMaxInboundConnections,
               DefaultMaxOutboundConnections = network.DefaultMaxOutboundConnections,
               DefaultPort = network.DefaultPort,
               DefaultRPCPort = network.DefaultRPCPort,
               DNSSeeds = network.DNSSeeds.Select(s => s.Host).ToList(),
               FallbackFee = network.FallbackFee,
               GenesisDate = UnixUtils.UnixTimestampToDate(network.GenesisTime).ToUniversalDateTime(), // Returns Kind.Unspecified, so translate.
               GenesisHash = network.GenesisHash.ToString(),
               MinRelayTxFee = network.MinRelayTxFee,
               MinTxFee = network.MinTxFee,
               Name = network.Name,
               NetworkType = network.NetworkType,
               SeedNodes = network.SeedNodes.Select(s => s.Endpoint.ToString()).ToList(),

               Consensus = new ConsensusInfo
               {
                  CoinbaseMaturity = consensus.CoinbaseMaturity,
                  MaxReorgLength = consensus.MaxReorgLength,
                  CoinType = consensus.CoinType,
                  IsProofOfStake = consensus.IsProofOfStake,
                  LastPOWBlock = consensus.LastPOWBlock,
                  MaxMoney = consensus.MaxMoney,
                  PremineReward = consensus.PremineReward.ToUnit(NBitcoin.MoneyUnit.BTC),
                  ProofOfStakeReward = consensus.ProofOfStakeReward.ToUnit(NBitcoin.MoneyUnit.BTC),
                  ProofOfWorkReward = consensus.ProofOfWorkReward.ToUnit(NBitcoin.MoneyUnit.BTC),
                  TargetSpacing = consensus.TargetSpacing
               } };
         }

         return coinInfo;
      }

      public async Task<Statistics> Statistics()
      {
         if (globalState.StoreTip?.BlockHash == null)
         {
            return new Statistics { Symbol = syncConnection.Symbol, Error = "node is not ready"};
         }

         var cachedStats = cache.Get<Statistics>(StatisticsKey);

         if (cachedStats != null &&
             cachedStats.Blockchain.BestBlockHash == globalState.StoreTip.BlockHash)
         {
            return cachedStats;
         }


         SyncConnection connection = syncConnection;
         var client = clientFactory.Create(connection);
         var stats = new Statistics { Symbol = syncConnection.Symbol };

         try
         {
            stats.Blockchain = await client.GetBlockchainInfo();
            stats.Network = await client.GetNetworkInfo();
         }
         catch (Exception ex)
         {
            stats.Error = ex.Message;
            return stats;
         }

         stats.TransactionsInPool = storage.GetMemoryTransactionsCount();

         try
         {
            SyncBlockInfo latestBlock = storage.GetLatestBlock();

            if (latestBlock != null)
            {
               stats.SyncBlockIndex = latestBlock.BlockIndex;
               stats.Progress = $"{stats.SyncBlockIndex}/{stats.Blockchain.Blocks} - {stats.Blockchain.Blocks - stats.SyncBlockIndex}";
               stats.BlocksLeftToSync = stats.Blockchain.Blocks - stats.SyncBlockIndex;

               if (globalState.IndexMode)
               {
                  stats.Progress = $"{stats.Progress} (indexing)";
               }

               double totalSeconds = syncConnection.RecentItems.Sum(s => s.Duration.TotalSeconds);
               stats.AvgBlockPersistInSeconds = Math.Round(totalSeconds / syncConnection.RecentItems.Count, 2);

               long totalSize = syncConnection.RecentItems.Sum(s => s.Size);
               stats.AvgBlockSizeKb = Math.Round((double)totalSize / syncConnection.RecentItems.Count, 0);

               stats.BlocksPerMinute = syncConnection.RecentItems.Count(w => w.Inserted > DateTime.UtcNow.AddMinutes(-1));
            }
         }
         catch (Exception ex)
         {
            stats.Progress = ex.Message;
         }

         stats.IsInIBDMode = globalState.IbdMode();

         cache.Set(StatisticsKey,stats);

         return stats;
      }

      public async Task<List<PeerInfo>> Peers()
      {
         SyncConnection connection = syncConnection;
         var client = clientFactory.Create(connection);
         var res = (await client.GetPeerInfo()).ToList();

         res.ForEach(p =>
         {
            if (TryParse(p.Addr, out IPEndPoint ipe))
            {
               string addr = ipe.Address.ToString();
               if (ipe.Address.IsIPv4MappedToIPv6)
               {
                  addr = ipe.Address.MapToIPv4().ToString();
               }

               p.Addr = $"{addr}:{ipe.Port}";
            }
         });

         return res;
      }

      public async Task<FeeEstimations> GetFeeEstimation(params int[] confirmations)
      {
         IBlockchainClient client = null;

         var feeEstimations = new FeeEstimations { Fees = new List<FeeEstimation>() };

         foreach (int confirmation in confirmations)
         {
            var key = $"{FeeEstimationKey}-{confirmation}";
            var cachedFees = cache.Get<FeeEstimation>($"{FeeEstimationKey}-{confirmation}");

            if (cachedFees != null)
            {
               feeEstimations.Fees.Add(cachedFees);
            }
            else
            {
               client ??= clientFactory.Create(syncConnection);

               EstimateSmartFeeResponse estimateSmartFee = null;

               try
               {
                  estimateSmartFee = await client.EstimateSmartFeeAsync(confirmation);
               }
               catch (Exception e)
               {
                  log.LogError(e, "fee call failed");
                  throw;
               }

               var feeEstimation = new FeeEstimation() { Confirmations = confirmation, FeeRateet = estimateSmartFee.FeeRate.FeePerK.Satoshi, };

               feeEstimations.Fees.Add(feeEstimation);

               log.LogInformation($"Requested fee estimation for {confirmation} confirmations is {estimateSmartFee.FeeRate.FeePerK}");

               cache.Set(key, feeEstimation, TimeSpan.FromMinutes(1));
            }

         }

         return feeEstimations;
      }


      // TODO: Figure out the new alternative to MaxPort that can be used.
      // This code is temporary til Blockcore upgrades to netcore 3.3
      // see https://github.com/dotnet/corefx/pull/33119
      public const int MaxPort = 0x0000FFFF;

      public static bool TryParse(string s, out IPEndPoint result)
      {
         return TryParse(s.AsSpan(), out result);
      }


      public static bool TryParse(ReadOnlySpan<char> s, out IPEndPoint result)
      {
         int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
         int lastColonPos = s.LastIndexOf(':');

         // Look to see if this is an IPv6 address with a port.
         if (lastColonPos > 0)
         {
            if (s[lastColonPos - 1] == ']')
            {
               addressLength = lastColonPos;
            }
            // Look to see if this is IPv4 with a port (IPv6 will have another colon)
            else if (s.Slice(0, lastColonPos).LastIndexOf(':') == -1)
            {
               addressLength = lastColonPos;
            }
         }

         if (IPAddress.TryParse(s.Slice(0, addressLength), out IPAddress address))
         {
            uint port = 0;
            if (addressLength == s.Length ||
                (uint.TryParse(s.Slice(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort))

            {
               result = new IPEndPoint(address, (int)port);
               return true;
            }
         }

         result = null;
         return false;
      }
   }
}
