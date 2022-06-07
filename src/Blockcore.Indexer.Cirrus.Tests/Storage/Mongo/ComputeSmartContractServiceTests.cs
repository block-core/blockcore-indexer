using System;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Blockcore.Indexer.Cirrus.Tests.Storage.Mongo;

public class ComputeSmartContractServiceTests
{
   ComputeSmartContractService<DaoContractComputedTable> sut;

   CirrusMongoDbMock mongoDbMock;

   static Random Random = new();
   private static string NewRandomString => Guid.NewGuid().ToString();
   private static int NewRandomInt32 => Random.Next();

   public ComputeSmartContractServiceTests()
   {
      mongoDbMock = new CirrusMongoDbMock();

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


      sut = new ComputeSmartContractService<DaoContractComputedTable>(null, mongoDbMock.CirrusMongoDbObject,
         new Mock<ISmartContractHandlersFactory<DaoContractComputedTable>>().Object, new Mock<ICryptoClientFactory>().Object, syncConnection,
         Mock.Of<IMongoDatabase>(), new Mock<ISmartContractTransactionsLookup<DaoContractComputedTable>>().Object);
   }

   //[Fact]
   public async Task WhenTheContractIsNotFountAnTheTrandactionIsNotFoundReturnsNull()
   {
      var result = await sut.ComputeSmartContractForAddressAsync(Guid.NewGuid().ToString());


      Assert.Null(result);
   }
}
