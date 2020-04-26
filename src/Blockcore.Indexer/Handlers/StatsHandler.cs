namespace Blockcore.Indexer.Api.Handlers
{
   using System;
   using System.Collections.Generic;
   using System.Globalization;
   using System.Linq;
   using System.Net;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Api.Handlers.Types;
   using Blockcore.Indexer.Client;
   using Blockcore.Indexer.Client.Types;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage;
   using Microsoft.Extensions.Options;
   using Newtonsoft.Json;
   using Blockcore.Indexer.Extensions;

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

      /// <summary>
      /// Initializes a new instance of the <see cref="StatsHandler"/> class.
      /// </summary>
      public StatsHandler(
         SyncConnection connection, IStorage storage,
         IOptions<NetworkSettings> networkConfig,
         IOptions<IndexerSettings> configuration,
         IOptions<ChainSettings> chainConfiguration)
      {
         this.storage = storage;
         syncConnection = connection;
         this.configuration = configuration.Value;
         this.chainConfiguration = chainConfiguration.Value;
         this.networkConfig = networkConfig.Value;
      }

      public async Task<StatsConnection> StatsConnection()
      {
         SyncConnection connection = syncConnection;
         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         int clientConnection = await client.GetConnectionCountAsync();
         return new StatsConnection { Connections = clientConnection };
      }

      public async Task<CoinInfo> CoinInformation()
      {
         long index = storage.BlockGetBlockCount(1).FirstOrDefault()?.BlockIndex ?? 0;

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

         // If we have network type available, we'll extend with extra metadata.
         if (syncConnection.HasNetworkType)
         {
            NBitcoin.Network network = syncConnection.Network;
            NBitcoin.IConsensus consensus = network.Consensus;

            coinInfo.Network = new NetworkInfo {
               CoinTicker = network.CoinTicker,
               DefaultAPIPort = network.DefaultAPIPort,
               DefaultMaxInboundConnections = network.DefaultMaxInboundConnections,
               DefaultMaxOutboundConnections = network.DefaultMaxOutboundConnections,
               DefaultPort = network.DefaultPort,
               DefaultRPCPort = network.DefaultRPCPort,
               DefaultSignalRPort = network.DefaultSignalRPort,
               DNSSeeds = network.DNSSeeds.Select(s => s.Host).ToList(),
               FallbackFee = network.FallbackFee,
               GenesisDate = UnixUtils.UnixTimestampToDate(network.GenesisTime).ToUniversalDateTime(), // Returns Kind.Unspecified, so translate.
               GenesisHash = network.GenesisHash.ToString(),
               MinRelayTxFee = network.MinRelayTxFee,
               MinTxFee = network.MinTxFee,
               Name = network.Name,
               NetworkType = network.NetworkType,
               SeedNodes = network.SeedNodes.Select(s => s.Endpoint.ToString()).ToList(),

               Consensus = new ConsensusInfo {
                  CoinbaseMaturity = consensus.CoinbaseMaturity,
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
         SyncConnection connection = syncConnection;
         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);
         var stats = new Statistics { Symbol = syncConnection.Symbol };

         try
         {
            stats.BlockchainInfo = await client.GetBlockchainInfo();
            stats.NetworkInfo = await client.GetNetworkInfo();
         }
         catch (Exception ex)
         {
            stats.Error = ex.Message;
            return stats;
         }

         stats.TransactionsInPool = storage.GetMemoryTransactions().Count();

         try
         {
            stats.SyncBlockIndex = storage.BlockGetBlockCount(1).First().BlockIndex;
            stats.Progress = $"{stats.SyncBlockIndex}/{stats.BlockchainInfo.Blocks} - {stats.BlockchainInfo.Blocks - stats.SyncBlockIndex}";

            double totalSeconds = syncConnection.RecentItems.Sum(s => s.Duration.TotalSeconds);
            stats.AvgBlockPersistInSeconds = Math.Round(totalSeconds / syncConnection.RecentItems.Count, 2);

            long totalSize = syncConnection.RecentItems.Sum(s => s.Size);
            stats.AvgBlockSizeKb = Math.Round((double)totalSize / syncConnection.RecentItems.Count, 0);

            stats.BlocksPerMinute = syncConnection.RecentItems.Count(w => w.Inserted > DateTime.UtcNow.AddMinutes(-1));
         }
         catch (Exception ex)
         {
            stats.Progress = ex.Message;
         }

         return stats;
      }

      public async Task<List<PeerInfo>> Peers()
      {
         SyncConnection connection = syncConnection;
         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);
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
