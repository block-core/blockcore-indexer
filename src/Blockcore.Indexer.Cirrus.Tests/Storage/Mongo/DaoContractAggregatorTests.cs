using System;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Blockcore.Indexer.Cirrus.Tests.Storage.Mongo;

public class DaoContractAggregatorTests
{
   DaoContractAggregator sut;

   static Random Random = new();
   private static string NewRandomString => Guid.NewGuid().ToString();
   private static int NewRandomInt32 => Random.Next();

   public DaoContractAggregatorTests()
   {
      var indexSettingsMock = new Mock<IOptions<IndexerSettings>>();

      var indexSettings = new IndexerSettings
      {
         RpcPassword = NewRandomString,
         RpcUser = NewRandomString,
         RpcAccessPort = NewRandomInt32,
         RpcDomain = NewRandomString,
      };

      indexSettingsMock.Setup(_ => _.Value)
         .Returns(() => indexSettings);
      var chainSetting = new Mock<IOptions<ChainSettings>>();

      chainSetting.SetupGet(_ => _.Value)
         .Returns(new ChainSettings { Symbol = NewRandomString });

      var networkSettings = new Mock<IOptions<NetworkSettings>>();

      networkSettings.Setup(_ => _.Value)
         .Returns(new NetworkSettings
         {
            APIPort = NewRandomInt32,
            NetworkConsensusFactoryType = typeof(ConsensusFactory).AssemblyQualifiedName,
            NetworkWitnessPrefix = "coin"
         });

      var syncConnection = new SyncConnection(indexSettingsMock.Object,
         chainSetting.Object, networkSettings.Object);


      sut = new DaoContractAggregator(null, new Mock<ICirrusMongoDb>().Object,
         new Mock<ILogReaderFactory>().Object, new Mock<ICryptoClientFactory>().Object, syncConnection);
   }

   [Fact]
   public async Task WhenTheContractIsNotFountAnTheTrandactionIsNotFoundReturnsNull()
   {
      var result = await sut.ComputeDaoContractForAddressAsync(Guid.NewGuid().ToString());


      Assert.Null(result);
   }
}
