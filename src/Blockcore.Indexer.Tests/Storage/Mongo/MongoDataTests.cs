using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
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


      var scriptInterpeter = new Mock<IScriptInterpreter>();

      scriptInterpeter.Setup(_ => _.InterpretScript(It.IsAny<Network>(), It.IsAny<Script>()))
         .Returns(() => scriptOutputInfo);

      mongodbMock = new MongodbMock();

      sut = new MongoData(null, syncConnection, chainSetting.Object, globalState,
            new MapMongoBlockToStorageBlock(),
            cryptoClientFactory.Object, scriptInterpeter.Object, mongodbMock.MongoDatabaseObject,
            mongodbMock.MongoDbObject, new Mock<IBlockRewindOperation>().Object,null);
   }

   // TODO dan: fix this test
    [Fact]
   public void GetUnspentTransactionsByAddressWithItemsInMempool()
   {
      var addressMain = NewRandomString;
      var addressSecondery = NewRandomString;

      globalState.StoreTip = new Core.Storage.Types.SyncBlockInfo { BlockIndex = 100 };

      var outpoint1 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput1 = new UnspentOutputTable { Outpoint = outpoint1, Address = addressMain, Value = NewRandomInt64, BlockIndex = 100 };
      var outpoint2 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput2 = new UnspentOutputTable { Outpoint = outpoint2, Address = addressMain, Value = NewRandomInt64, BlockIndex = 100 };
      var outpoint3 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput3 = new UnspentOutputTable { Outpoint = outpoint3, Address = addressSecondery, Value = NewRandomInt64, BlockIndex = 100 };

      var outpoint4 = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString }; // not returned from the utxo table

      var mempoolTable = new MempoolTable
      {
         TransactionId = NewRandomString,
         Inputs = new List<MempoolInput>
         {
            new() { Address = addressMain, Outpoint = outpoint1, Value = NewRandomInt64 },
            new() { Address = addressMain, Outpoint = outpoint4, Value = NewRandomInt64 },
            new() { Address = addressSecondery, Outpoint = outpoint3, Value = NewRandomInt64 }
         },
         Outputs = new List<MempoolOutput>
         {
            new() { Address = addressMain, Value = NewRandomInt64 },
            new() { Address = addressSecondery, Value = NewRandomInt64 }
         }
      };

      OutputTable outputTable1 = new OutputTable
      {
         Address = addressMain,
         Outpoint = unspentOutput2.Outpoint,
         Value = unspentOutput2.Value,
      };

      globalState.LocalMempoolView = new ConcurrentDictionary<string, string>();
      globalState.LocalMempoolView.TryAdd(mempoolTable.TransactionId, string.Empty);

      mongodbMock.GivenTheAggregateListReturnsTheExpectedSet(mongodbMock.unspentOutputTableCollection,
         new List<UnspentOutputTable> { unspentOutput1, unspentOutput2, unspentOutput3 });

      mongodbMock.GivenTheAggregateCountReturnsTheExpectesSet(mongodbMock.unspentOutputTableCollection, 3);

      mongodbMock.GivenTheAggregateListAsyncReturnsTheExpectedSet(mongodbMock.outputTableCollection,  new List<OutputTable>() { outputTable1 });

      mongodbMock.GivenTheAggregateListReturnsTheExpectedSet(mongodbMock.mempoolTable, new List<MempoolTable> { mempoolTable });

      var res = sut.GetUnspentTransactionsByAddressAsync(addressMain, 0, 0, 10).Result;

      Assert.Equal(2, res.Items.Count());

      Assert.Equal(outputTable1.Outpoint.TransactionId, res.Items.ElementAt(0).Outpoint.TransactionId);
      Assert.Equal(outputTable1.Outpoint.OutputIndex, res.Items.ElementAt(0).Outpoint.OutputIndex);
      Assert.Equal(outputTable1.Value, res.Items.ElementAt(0).Value);
      Assert.Equal(addressMain, res.Items.ElementAt(0).Address);

      Assert.Equal(mempoolTable.TransactionId, res.Items.ElementAt(1).Outpoint.TransactionId);
      Assert.Equal(0, res.Items.ElementAt(1).Outpoint.OutputIndex);
      Assert.Equal(mempoolTable.Outputs[0].Value, res.Items.ElementAt(1).Value);
      Assert.Equal(addressMain, res.Items.ElementAt(1).Address);
   }
}
