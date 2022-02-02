using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Networks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Indexer.Tests.Storage.Mongo;

public class MongoStorageOperationsTests
{
   MongoStorageOperations sut;

   static Random Random = new();
   private static string NewRandomString => Guid.NewGuid().ToString();
   private static int NewRandomInt32 => Random.Next();
   private static long NewRandomInt64  => Random.NextInt64();

   IndexerSettings indexSettings;
   ScriptOutputInfo scriptOutputInfo;
   public MongoStorageOperationsTests()
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

      var globalState = new GlobalState();

      var cryptoClientFactory = new Mock<ICryptoClientFactory>();

      var mongodatabase = new Mock<IMongoDatabase>();

      mongodatabase.Setup(_ => _.Client)
         .Returns(new Mock<IMongoClient>().Object);

      var scriptInterpeter = new Mock<IScriptInterpeter>();

      scriptInterpeter.Setup(_ => _.InterpretScript(It.IsAny<Network>(), It.IsAny<Script>()))
         .Returns(() => scriptOutputInfo);

      sut = new MongoStorageOperations(syncConnection,
         new MongoData(null, syncConnection, indexSettingsMock.Object,
            chainSetting.Object, globalState, new MapMongoBlockToStorageBlock(),
            cryptoClientFactory.Object, scriptInterpeter.Object, mongodatabase.Object),
         new UtxoCache(null), indexSettingsMock.Object, globalState, new MapMongoBlockToStorageBlock(),
         scriptInterpeter.Object);
   }

   private static BlockInfo NewRandomBlockInfo => new()
   {
      Size = NewRandomInt64,
      Bits = NewRandomString,
      Confirmations = NewRandomInt32,
      Hash = NewRandomString,
      Height = NewRandomInt32,
      Merkleroot = NewRandomString,
      Nonce = NewRandomInt64,
      Time = NewRandomInt64,
      Transactions = new List<string>(0),
      Version = NewRandomInt64,
      PosFlags = NewRandomString,
      PosModifierv2 = NewRandomString,
      NextBlockHash = NewRandomString,
      PosBlockSignature = NewRandomString,
      PosBlockTrust = NewRandomString,
      PosChainTrust = NewRandomString,
      PosHashProof = NewRandomString,
      PreviousBlockHash = NewRandomString,
   };

   static SyncBlockTransactionsOperation WithRandomSyncBlockTransactionsOperation()
   {
      var item = new SyncBlockTransactionsOperation
      {
         BlockInfo = NewRandomBlockInfo,
         Transactions = new List<Transaction>
         {
            new Transaction() //TODO David generate random transactions
         }
      };
      return item;
   }

   [Fact]
   public void WhenAddToStorageIsCalledSetsTheTotalSizeFromBlockInfo()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      sut.AddToStorageBatch(batch,item);

      batch.TotalSize.Should().Be(item.BlockInfo.Size);
   }

   [Fact]
   public void WhenAddToStorageIsCalledSetsTheBlockTableFromBlockInfo()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      sut.AddToStorageBatch(batch,item);

      batch.BlockTable.Keys.Single().Should().Be(item.BlockInfo.Height);
      batch.BlockTable.Values.Should().HaveCount(1);
      var blockTable = batch.BlockTable.Values.Single();
      blockTable.Should().BeEquivalentTo(item.BlockInfo,
         _ => _.ExcludingMissingMembers());
      //TODO change to WithMapping on the next version of fluent assertion
      blockTable.BlockHash.Should().Be(item.BlockInfo.Hash);
      blockTable.BlockIndex.Should().Be(item.BlockInfo.Height);
      blockTable.BlockSize.Should().Be(item.BlockInfo.Size);
      blockTable.BlockTime.Should().Be(item.BlockInfo.Time);
   }

   [Fact]
   public void WhenAddToStorageIsCalledSetsTheTransactionBlockTableFromTransactions()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      sut.AddToStorageBatch(batch,item);

      batch.TransactionBlockTable.Should().HaveCount(1);

      batch.TransactionBlockTable.Single().Should().BeEquivalentTo(new TransactionBlockTable
      {
         BlockIndex = item.BlockInfo.Height,
         TransactionId = item.Transactions.Single().GetHash().ToString()
      });
   }

   [Fact]
   public void WhenAddToStorageIsCalledWithStoreRawTransactionsTrueSetsTransactionTable()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      indexSettings.StoreRawTransactions = true;

      sut.AddToStorageBatch(batch,item);

      batch.TransactionBlockTable.Should().HaveCount(1);

      batch.TransactionTable.Single().Should().BeEquivalentTo(new TransactionTable
      {
         RawTransaction = new byte[]{0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
         TransactionId = "d21633ba23f70118185227be58a63527675641ad37967e2aa461559f577aec43"
      });
   }



   [Fact]
   public void WhenAddToStorageIsCalledSetsTheOutputsInTheTransactionToTheOutputTable()
   {
      var batch = new StorageBatch();
      var valueMoney = new Money(NewRandomInt32);
      var script = new Script(NewRandomString.Replace('-','1'));
      var item = WithRandomSyncBlockTransactionsOperation();
      item.Transactions = new List<Transaction>
      {
         new()
         {
            Outputs =
            {
               new TxOut { Value = valueMoney, ScriptPubKey =  script}
            }
         }
      };

      scriptOutputInfo = new ScriptOutputInfo { Addresses = new[] { NewRandomString } };

      var expectedOutpoint = new Outpoint
      {
         OutputIndex = 0, TransactionId = item.Transactions.Single().GetHash().ToString()
      };


      sut.AddToStorageBatch(batch, item);


      batch.OutputTable.Should().HaveCount(1);

      var output = batch.OutputTable.Single();

      output.Key.Should().Be(expectedOutpoint.ToString());

      output.Value.Should().BeEquivalentTo(new OutputTable
      {
         Address = scriptOutputInfo.Addresses.Single(),
         Outpoint = expectedOutpoint,
         Value = valueMoney.Satoshi,
         BlockIndex = item.BlockInfo.Height,
         CoinBase = false,
         CoinStake = false,
         ScriptHex = script.ToHex()
      });
   }

   [Fact]
   public void WhenAddToStorageIsCalledSetsTheInputsInInputTableWithoutAddress()
   {
      var batch = new StorageBatch();
      var hash = new uint256($"{NewRandomInt64}{NewRandomInt64}{NewRandomInt64}{NewRandomInt64}".Substring(0,64));
      int n = NewRandomInt32;
      var expectedOutpoint = new OutPoint
      {
         Hash = hash,
         N = (uint)n
      };
      var item = WithRandomSyncBlockTransactionsOperation();
      item.Transactions = new List<Transaction>
      {
         new()
         {
            Inputs =
            {
               new TxIn { PrevOut = expectedOutpoint}
            }
         }
      };

      scriptOutputInfo = new ScriptOutputInfo { Addresses = new[] { NewRandomString } };


      sut.AddToStorageBatch(batch, item);


      batch.InputTable.Should().HaveCount(1);

      var input = batch.InputTable.Single();

      input.Should().BeEquivalentTo(new InputTable
      {
         Address = null,
         Outpoint = new Outpoint{TransactionId = hash.ToString(),OutputIndex = n},
         Value = 0,
         BlockIndex = item.BlockInfo.Height,
         TrxHash = item.Transactions.Single().GetHash().ToString()
      });
   }
}
