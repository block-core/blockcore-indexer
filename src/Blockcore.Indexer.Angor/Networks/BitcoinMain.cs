using Blockcore.Consensus;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.DataEncoders;
using Blockcore.NBitcoin.Protocol;
using Blockcore.Networks;

namespace Blockcore.Indexer.Angor.Networks
{
    public class BitcoinMain : Network
    {
        public BitcoinMain()
        {
            Name = "Main";
            AdditionalNames = new List<string> { "Mainnet" };
            NetworkType = NetworkType.Mainnet;

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            Magic = 0xD9B4BEF9;
            DefaultPort = 8333;
            DefaultMaxOutboundConnections = 8;
            DefaultMaxInboundConnections = 117;
            DefaultRPCPort = 8332;
            DefaultAPIPort = 37220;
            MinTxFee = 1000;
            MaxTxFee = Money.Coins(0.1m).Satoshi;
            FallbackFee = 20000;
            MinRelayTxFee = 1000;
            CoinTicker = "BTC";
            DefaultBanTimeSeconds = 60 * 60 * 24; // 24 Hours

            var consensusFactory = new ConsensusFactory();



            consensusFactory.Protocol = new ConsensusProtocol()
            {
                ProtocolVersion = ProtocolVersion.FEEFILTER_VERSION,
                MinProtocolVersion = ProtocolVersion.SENDHEADERS_VERSION,
            };

            Consensus = new Blockcore.Consensus.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: new ConsensusOptions(), // Default - set to Bitcoin params.
                coinType: 0,
                hashGenesisBlock: null,
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: null,
                bip9Deployments: null,
                bip34Hash: null,
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
                maxReorgLength: 0,
                defaultAssumeValid: null, // 629000
                maxMoney: 21000000 * Money.COIN,
                coinbaseMaturity: 100,
                premineHeight: 0,
                premineReward: Money.Zero,
                proofOfWorkReward: Money.Coins(50),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                targetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: null,
                minimumChainWork: null,
                isProofOfStake: false,
                lastPowBlock: default(int),
                proofOfStakeLimit: null,
                proofOfStakeLimitV2: null,
                proofOfStakeReward: Money.Zero,
                proofOfStakeTimestampMask: 0
            );

            Base58Prefixes = new byte[12][];
            Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (0) };
            Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (5) };
            Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (128) };
            Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            var encoder = new Bech32Encoder("bc");
            Bech32Encoders = new Bech32Encoder[2];
            Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            StandardScriptsRegistry = new BitcoinStandardScriptsRegistry();

        }
    }
}
