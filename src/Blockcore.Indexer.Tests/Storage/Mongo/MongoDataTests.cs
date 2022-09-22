using System;
using Blockcore.Consensus;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Networks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Blockcore.Indexer.Tests.Storage.Mongo;

public class MongoDataTests
{
   readonly MongoData sut;

   static readonly Random Random = new();
   private static string NewRandomString => Guid.NewGuid().ToString();
   private static int NewRandomInt32 => Random.Next();
   private static long NewRandomInt64  => Random.NextInt64();

   readonly IndexerSettings indexSettings;
   ScriptOutputInfo scriptOutputInfo;
   readonly MongodbMock mongodbMock;
   GlobalState globalState;

   public MongoDataTests()
   {
      var indexSettingsMock = new Mock<IOptions<IndexerSettings>>();

      indexSettings = new IndexerSettings
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

      globalState = new GlobalState();

      var cryptoClientFactory = new Mock<ICryptoClientFactory>();


      var scriptInterpeter = new Mock<IScriptInterpeter>();

      scriptInterpeter.Setup(_ => _.InterpretScript(It.IsAny<Network>(), It.IsAny<Script>()))
         .Returns(() => scriptOutputInfo);

      mongodbMock = new MongodbMock();

      sut = new MongoData(null, syncConnection, chainSetting.Object, globalState,
            new MapMongoBlockToStorageBlock(),
            cryptoClientFactory.Object, scriptInterpeter.Object, mongodbMock.MongoDatabaseObject,
            mongodbMock.MongoDbObject, new Mock<IBlockRewindOperation>().Object);
   }

   // TODO dan: fix this test
   // [Fact] 
   public void GetUnspentTransactionsByAddressWithItemsInMempool()
   {
      var addressMain = NewRandomString;
      var addressSecondery = NewRandomString;

      globalState.StoreTip = new Core.Storage.Types.SyncBlockInfo { BlockIndex = 100 };

      var outpoint1 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput1 = new UnspentOutputTable { Outpoint = outpoint1, Address = NewRandomString, Value = NewRandomInt64, BlockIndex = 100 };
      var outpoint2 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput2 = new UnspentOutputTable { Outpoint = outpoint2, Address = NewRandomString, Value = NewRandomInt64, BlockIndex = 100 };
      var outpoint3 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput3 = new UnspentOutputTable { Outpoint = outpoint3, Address = addressSecondery, Value = NewRandomInt64, BlockIndex = 100 };

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.unspentOutputTableCollection, unspentOutput1);
      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.unspentOutputTableCollection, unspentOutput2);
      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.unspentOutputTableCollection, unspentOutput3);


      var mempoolTable = new MempoolTable
      {
         TransactionId = NewRandomString,
      };

      mempoolTable.Inputs.Add(new MempoolInput { Address = addressMain, Outpoint = outpoint1, Value = 5 });
      mempoolTable.Inputs.Add(new MempoolInput { Address = addressSecondery, Outpoint = outpoint2, Value = 5 });
      mempoolTable.Outputs.Add(new MempoolOutput { Address = addressMain, Value = 5 });
      mempoolTable.Outputs.Add(new MempoolOutput { Address = addressSecondery, Value = 5 });

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.mempoolTable, mempoolTable);

      var res = sut.GetUnspentTransactionsByAddressAsync(addressMain, 0, 0, 10).Result;
   }
}
