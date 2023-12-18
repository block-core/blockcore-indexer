using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
using Blockcore.NBitcoin;
using Blockcore.Networks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace Blockcore.Indexer.Tests.Storage.Mongo;

public class MongoStorageOperationsTests
{
   readonly MongoStorageOperations sut;

   static readonly Random Random = new();
   private static string NewRandomString => Guid.NewGuid().ToString();
   private static int NewRandomInt32 => Random.Next();
   private static long NewRandomInt64  => Random.NextInt64();

   readonly IndexerSettings indexSettings;
   ScriptOutputInfo scriptOutputInfo;
   readonly MongodbMock mongodbMock;

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


      var scriptInterpeter = new Mock<IScriptInterpreter>();

      scriptInterpeter.Setup(_ => _.InterpretScript(It.IsAny<Network>(), It.IsAny<Script>()))
         .Returns(() => scriptOutputInfo);

      mongodbMock = new MongodbMock();

      sut = new MongoStorageOperations(syncConnection,
         mongodbMock.MongoDbObject,
         new UtxoCache(null),
         indexSettingsMock.Object,
         globalState,
         new MapMongoBlockToStorageBlock(),
         scriptInterpeter.Object,
         new MongoData(null, syncConnection, chainSetting.Object, globalState,
            new MapMongoBlockToStorageBlock(),
            cryptoClientFactory.Object, scriptInterpeter.Object, mongodbMock.MongoDatabaseObject,
            mongodbMock.MongoDbObject, new Mock<IBlockRewindOperation>().Object,null));
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

   private static BlockTable NewRandomBlockTable => new()
   {
      BlockSize = NewRandomInt64,
      Bits = NewRandomString,
      Confirmations = NewRandomInt32,
      BlockHash = NewRandomString,
      BlockIndex = NewRandomInt32,
      Merkleroot = NewRandomString,
      Nonce = NewRandomInt64,
      BlockTime = NewRandomInt64,
      TransactionCount = NewRandomInt32,
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

   StorageBatch WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully()
   {
      var batch = new StorageBatch();

      var block = NewRandomBlockTable;
      batch.BlockTable.Add(block.BlockIndex, block);

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.blockTableCollection,
         block);

      return batch;
   }

   void WithASuccessfulDeleteManyOnUnspentOutputTable(StorageBatch batch)
   {
      var utxo = batch.InputTable.Select(_ => _.Outpoint).ToList();

      var filterToUpdate = Builders<UnspentOutputTable>.Filter
         .Where(_ => utxo.Contains(_.Outpoint));

      mongodbMock.WithTheDocumentsDeletedSuccessfullyInMongoDb(mongodbMock.unspentOutputTableCollection, filterToUpdate,
          new DeleteResult.Acknowledged(utxo.Count));
   }

   [Fact]
   public void AddToStorageBatchSetsTheTotalSizeFromBlockInfo()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      sut.AddToStorageBatch(batch,item);

      batch.TotalSize.Should().Be(item.BlockInfo.Size);
   }

   [Fact]
   public void AddToStorageBatchSetsTheBlockTableFromBlockInfo()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      sut.AddToStorageBatch(batch,item);

      batch.BlockTable.Keys.Single().Should().Be(item.BlockInfo.Height);
      batch.BlockTable.Values.Should().ContainSingle();

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
   public void AddToStorageBatchSetsTheTransactionBlockTableFromTransactions()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      sut.AddToStorageBatch(batch,item);

      batch.TransactionBlockTable.Should().ContainSingle()
         .Which.Should().BeEquivalentTo(new TransactionBlockTable
      {
         BlockIndex = item.BlockInfo.HeightAsUint32,
         TransactionId = item.Transactions.Single().GetHash().ToString()
      });
   }

   [Fact]
   public void AddToStorageBatchWithStoreRawTransactionsTrueSetsTransactionTable()
   {
      var batch = new StorageBatch();

      var item = WithRandomSyncBlockTransactionsOperation();

      indexSettings.StoreRawTransactions = true;

      sut.AddToStorageBatch(batch,item);

      batch.TransactionTable.Should().ContainSingle()
         .Which.Should().BeEquivalentTo(new TransactionTable
      {
         RawTransaction = new byte[]{0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
         TransactionId = "d21633ba23f70118185227be58a63527675641ad37967e2aa461559f577aec43"
      });
   }



   [Fact]
   public void AddToStorageBatchSetsTheOutputsInTheTransactionToTheOutputTable()
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


      batch.OutputTable.Should().ContainSingle()
         .Which.Deconstruct(out string key,out OutputTable output);

      key.Should().Be(expectedOutpoint.ToString());

      output.Should().BeEquivalentTo(new OutputTable
      {
         Address = scriptOutputInfo.Addresses.Single(),
         Outpoint = expectedOutpoint,
         Value = valueMoney.Satoshi,
         BlockIndex = item.BlockInfo.HeightAsUint32,
         CoinBase = false,
         CoinStake = false,
         ScriptHex = script.ToHex()
      });
   }

   [Fact]
   public void AddToStorageBatchSetsTheInputsInInputTableWithoutAddress()
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


      batch.InputTable.Should().ContainSingle()
         .Which.Should().BeEquivalentTo(new InputTable
      {
         Address = null,
         Outpoint = new Outpoint{TransactionId = hash.ToString(),OutputIndex = n},
         Value = 0,
         BlockIndex = item.BlockInfo.HeightAsUint32,
         TrxHash = item.Transactions.Single().GetHash().ToString()
      });
   }

   [Fact]
   public void AddToStorageBatchSetsTheInputsInInputTableWithAddressAndValue()
   {
      var batch = new StorageBatch();
      var valueMoney = new Money(NewRandomInt32);
      var script = new Script(NewRandomString.Replace('-','1'));
      var transaction = new Transaction { Outputs = { { new TxOut { Value = valueMoney, ScriptPubKey = script } } } };
      var item = WithRandomSyncBlockTransactionsOperation();
      item.Transactions = new List<Transaction>
      {
         transaction,
         new() { Inputs = { new TxIn { PrevOut = new OutPoint { Hash = transaction.GetHash(), N = 0 } } } }
      };

      scriptOutputInfo = new ScriptOutputInfo { Addresses = new[] { NewRandomString } };


      sut.AddToStorageBatch(batch, item);


      batch.InputTable.Should().ContainSingle()
         .Which.Should().BeEquivalentTo(new InputTable
      {
         Address = scriptOutputInfo.Addresses.Single(),
         Outpoint = new Outpoint{TransactionId = transaction.GetHash().ToString(),OutputIndex = 0},
         Value = valueMoney,
         BlockIndex = item.BlockInfo.HeightAsUint32,
         TrxHash = item.Transactions.Last().GetHash().ToString()
      });
   }

   [Fact]
   public void PushStorageBatchAddsBlockTableFromBatchToMongoDb()
   {
      var batch = new StorageBatch();

      var block = NewRandomBlockTable;
      batch.BlockTable.Add(block.BlockIndex, block);

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.blockTableCollection,
         block);

      sut.PushStorageBatch(batch);

      mongodbMock.ThanTheCollectionStoredTheItemsSuccessfullyAsynchronasly(mongodbMock.blockTableCollection,
         batch.BlockTable.Values);
   }

   [Fact]
   public void PushStorageBatchAddsTransactionBlockTableFromBatchToMongoDb()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      var blockTable = new TransactionBlockTable
      {
         BlockIndex = (uint)NewRandomInt32,
         TransactionId = NewRandomString
      };

      batch.TransactionBlockTable.Add(blockTable);

      sut.PushStorageBatch(batch);

      mongodbMock.ThanTheCollectionStoredTheItemsSuccessfullyAsynchronasly(mongodbMock.transactionBlockTableCollection,
         batch.TransactionBlockTable);
   }

   [Fact]
   public void PushStorageBatchAddsOutputTableFromBatchToMongoDb()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      var outputTable = new OutputTable()
      {
         BlockIndex = (uint)NewRandomInt32,
         Address = NewRandomString,
         Outpoint = new Outpoint{OutputIndex = NewRandomInt32,TransactionId = NewRandomString},
         Value = NewRandomInt64,
         CoinBase = NewRandomInt32 % 2 > 0,
         CoinStake = NewRandomInt32 % 2 > 0,
         ScriptHex = NewRandomString
      };

      batch.OutputTable.Add(outputTable.Outpoint.ToString(),outputTable);

      sut.PushStorageBatch(batch);

      mongodbMock.ThanTheCollectionStoredTheItemsSuccessfullyAsynchronasly(mongodbMock.outputTableCollection,
         batch.OutputTable.Values);
   }

   [Fact]
   public void PushStorageBatchAddsInputTableAddsToMongodb()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      //We must have an unspent output for an input that is being processed
      var outpoint = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput =
         new UnspentOutputTable { Outpoint = outpoint, Address = NewRandomString, Value = NewRandomInt64 };

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.unspentOutputTableCollection,
         unspentOutput);

      var inputTable = new InputTable
      {
         BlockIndex = (uint)NewRandomInt32,
         Outpoint = outpoint,
         TrxHash = NewRandomString
      };

      batch.InputTable.Add(inputTable);

      WithASuccessfulDeleteManyOnUnspentOutputTable(batch);

      sut.PushStorageBatch(batch);

      mongodbMock.ThanTheCollectionStoredTheItemsSuccessfullyAsynchronasly(mongodbMock.inputTableCollection,
         batch.InputTable);

      mongodbMock.ThanTheCollectionStoredTheUnorderedItemsSuccessfully(mongodbMock.inputTableCollection,
         _
            => _.Count() == 1 &&
               _.First().Address == unspentOutput.Address &&
               _.First().Value == unspentOutput.Value);
   }

   [Fact]
   public void PushStorageBatchAddsTransactionTableDataToMongodb()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      var transactionTable = new TransactionTable()
         {
            TransactionId = NewRandomString, RawTransaction = BitConverter.GetBytes(NewRandomInt64)
         };

      batch.TransactionTable.Add(transactionTable);

      sut.PushStorageBatch(batch);

      mongodbMock.ThanTheCollectionStoredTheItemsSuccessfully(mongodbMock.transactionTable,
         batch.TransactionTable);
   }

   //[Fact] TODO we need to add to the new List<BulkWriteError>{} an error with duplicate category to actually check the code otherwise it goes green as a false positive
#pragma warning disable xUnit1013 // Public method should be marked as test
   public void PushStorageBatchIgnoresDuplicatsOnInputTableForDuplicateKeyException()
#pragma warning restore xUnit1013 // Public method should be marked as test
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      var transactionTable = new TransactionTable()
      {
         TransactionId = NewRandomString, RawTransaction = BitConverter.GetBytes(NewRandomInt64)
      };


      mongodbMock.transactionTable.Setup(_ => _.InsertManyAsync(batch.TransactionTable, It.IsAny<InsertManyOptions>(),CancellationToken.None))
         .Throws(new MongoBulkWriteException<TransactionTable>(new ConnectionId(new ServerId(new ClusterId(),new IPEndPoint(256,1))) ,
            new BulkWriteResult<TransactionTable>.Acknowledged(NewRandomInt32,NewRandomInt64,NewRandomInt32,NewRandomInt32,null,new List<WriteModel<TransactionTable>>(),new List<BulkWriteUpsert>()),
            new List<BulkWriteError>{},null,new List<WriteModel<TransactionTable>>()));

      batch.TransactionTable.Add(transactionTable);

      Action callToSut = () => sut.PushStorageBatch(batch);

      callToSut.Should().NotThrow();

      mongodbMock.ThanTheCollectionStoredTheItemsSuccessfullyAsynchronasly(mongodbMock.transactionTable,
         batch.TransactionTable);
   }


   [Fact]
   public void PushStorageBatchAddsUnspendOutputToMongodbForEachOutputTableItem()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      var outputTable = new OutputTable
      {
         BlockIndex = (uint)NewRandomInt32,
         Address = NewRandomString,
         Outpoint = new Outpoint{OutputIndex = NewRandomInt32,TransactionId = NewRandomString},
         Value = NewRandomInt64,
         CoinBase = NewRandomInt32 % 2 > 0,
         CoinStake = NewRandomInt32 % 2 > 0,
         ScriptHex = NewRandomString
      };

      batch.OutputTable.Add(outputTable.Outpoint.ToString(),outputTable);

      sut.PushStorageBatch(batch);

      mongodbMock.ThanTheCollectionStoredTheUnorderedItemsSuccessfully(mongodbMock.unspentOutputTableCollection,
         _ => _.Count() == 1 &&
              _.First().Address == outputTable.Address &&
              _.First().Outpoint.ToString() == outputTable.Outpoint.ToString() &&
              _.First().Value == outputTable.Value &&
              _.First().BlockIndex == outputTable.BlockIndex);
   }

   [Fact]
   public void PushStorageBatchAddsDeletsFromUnspentOutputTableOnMongoDbForAllInputTableItems()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      //We must have an unspent output for an input that is being processed
      var outpoint = new Outpoint { OutputIndex = NewRandomInt32, TransactionId = NewRandomString };
      var unspentOutput =
         new UnspentOutputTable { Outpoint = outpoint, Address = NewRandomString, Value = NewRandomInt64 };

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.unspentOutputTableCollection,
         unspentOutput);

      var inputTable = new InputTable
      {
         BlockIndex = (uint)NewRandomInt32,
         Outpoint = outpoint,
         TrxHash = NewRandomString
      };

      batch.InputTable.Add(inputTable);

      WithASuccessfulDeleteManyOnUnspentOutputTable(batch);

      sut.PushStorageBatch(batch);

      var (docSerializer,serializer) = mongodbMock.GetRendererForDocumentExpresion<UnspentOutputTable>();

      var filterToDelete = Builders<UnspentOutputTable>.Filter
         .Where(_ => new List<Outpoint>{outpoint}.Contains(_.Outpoint));

      mongodbMock.unspentOutputTableCollection.Verify(_ =>
         _.DeleteMany(It.Is<ExpressionFilterDefinition<UnspentOutputTable>>(e =>
               e.Render(docSerializer,serializer) == filterToDelete.Render(docSerializer,serializer))
            , CancellationToken.None));
   }

   [Fact]
   public void PushStorageBatchUpdatesBlockTableItemsWithSyncCompleteTrueInMongodb()
   {
      StorageBatch batch = WithBatchThatHasABlockToPushAndUpdatesToSyncCompleteSuccessfully();

      sut.PushStorageBatch(batch);

      var (docSerializer,serializer) = mongodbMock.GetRendererForDocumentExpresion<BlockTable>();

      var filter = Builders<BlockTable>.Filter
         .Eq(_ => _.BlockIndex,batch.BlockTable.Single().Key);

      var update = Builders<BlockTable>.Update
         .Set(blockInfo => blockInfo.SyncComplete, true);

      mongodbMock.blockTableCollection.Verify(_ =>
            _.BulkWrite(It.Is<List<UpdateOneModel<BlockTable>>>(l =>
                     l.Single().Filter.Render(docSerializer,serializer) == filter.Render(docSerializer,serializer) &&
                     l.Single().Update.Render(docSerializer,serializer) == update.Render(docSerializer,serializer)),
               It.Is<BulkWriteOptions>(u => u.IsOrdered == true),
               CancellationToken.None),
         Times.Once);
   }

   [Fact]
   public void PushStorageBatchThrowsWhenTheBlockInertedIsNotTheSameHashAsTopBlockInMongodb()
   {
      var batch = new StorageBatch();

      var dbBlock = NewRandomBlockTable;
      var storageBlock = NewRandomBlockTable;

      batch.BlockTable.Add(storageBlock.BlockIndex, storageBlock);

      mongodbMock.GivenTheDocumentIsReturnedSuccessfullyFromMongoDb(mongodbMock.blockTableCollection,
         dbBlock);

      Action serviceCall = () => sut.PushStorageBatch(batch);

      serviceCall.Should().ThrowExactly<ArgumentException>();
   }
}
