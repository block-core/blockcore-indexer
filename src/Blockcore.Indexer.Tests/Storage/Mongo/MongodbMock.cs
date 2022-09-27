using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;

namespace Blockcore.Indexer.Tests.Storage.Mongo;

public class MongodbMock
{
   public Mock<IMongoCollection<BlockTable>> blockTableCollection;
   public Mock<IMongoCollection<TransactionBlockTable>> transactionBlockTableCollection;
   public Mock<IMongoCollection<OutputTable>> outputTableCollection;
   public Mock<IMongoCollection<UnspentOutputTable>> unspentOutputTableCollection;
   public Mock<IMongoCollection<InputTable>> inputTableCollection;
   public Mock<IMongoCollection<TransactionTable>> transactionTable;
   public Mock<IMongoCollection<MempoolTable>> mempoolTable;

   protected Mock<IMongoDatabase> mongodatabase;
   private readonly Mock<IMongoDb> db;

   public  MongodbMock()
   {
      db = new Mock<IMongoDb>();
      blockTableCollection = new Mock<IMongoCollection<BlockTable>>();
      transactionBlockTableCollection = new Mock<IMongoCollection<TransactionBlockTable>>();
      outputTableCollection = new Mock<IMongoCollection<OutputTable>>();
      unspentOutputTableCollection = new Mock<IMongoCollection<UnspentOutputTable>>();
      inputTableCollection = new Mock<IMongoCollection<InputTable>>();
      transactionTable = new Mock<IMongoCollection<TransactionTable>>();
      mempoolTable = new Mock<IMongoCollection<MempoolTable>>();

      db.Setup(_ => _.BlockTable).Returns(blockTableCollection.Object);
      db.Setup(_ => _.TransactionBlockTable).Returns(transactionBlockTableCollection.Object);
      db.Setup(_ => _.OutputTable).Returns(outputTableCollection.Object);
      db.Setup(_ => _.UnspentOutputTable).Returns(unspentOutputTableCollection.Object);
      db.Setup(_ => _.InputTable).Returns(inputTableCollection.Object);
      db.Setup(_ => _.TransactionTable).Returns(transactionTable.Object);
      db.Setup(_ => _.Mempool).Returns(mempoolTable.Object);

      mongodatabase = new Mock<IMongoDatabase>();
      mongodatabase.Setup(_ => _.GetCollection<BlockTable>("Block",null))
         .Returns(blockTableCollection.Object);
      mongodatabase.Setup(_ => _.GetCollection<TransactionBlockTable>("TransactionBlock",null))
         .Returns(transactionBlockTableCollection.Object);
      mongodatabase.Setup(_ => _.GetCollection<OutputTable>("Output",null))
         .Returns(outputTableCollection.Object);
      mongodatabase.Setup(_ => _.GetCollection<UnspentOutputTable>("UnspentOutput",null))
         .Returns(unspentOutputTableCollection.Object);
      mongodatabase.Setup(_ => _.GetCollection<InputTable>("Input",null))
         .Returns(inputTableCollection.Object);
      mongodatabase.Setup(_ => _.GetCollection<TransactionTable>("Transaction",null))
         .Returns(transactionTable.Object);
      mongodatabase.Setup(_ => _.GetCollection<MempoolTable>("Mempool", null))
         .Returns(mempoolTable.Object);

      mongodatabase.Setup(_ => _.Client)
         .Returns(new Mock<IMongoClient>().Object);
   }

   public IMongoDatabase MongoDatabaseObject => mongodatabase.Object;
   public IMongoDb MongoDbObject => db.Object;

   public void GivenTheDocumentIsReturnedSuccessfullyFromMongoDb<TDocument>(
      Mock<IMongoCollection<TDocument>> collection, TDocument document)
   {
      var lookup = new Mock<IAsyncCursor<TDocument>>();

      lookup.Setup(_ => _.Current).Returns(() => new[] { document });
      lookup.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
         .Returns(true)
         .Returns(false);

      collection.Setup(_ => _.FindSync(It.IsAny<FilterDefinition<TDocument>>(),
            It.IsAny<FindOptions<TDocument, TDocument>>()
            , It.IsAny<CancellationToken>()))
         .Returns(() => lookup.Object);
   }

   public void WithTheDocumentsUpdatedSuccessfullyInMongoDb<TDocument>(
      Mock<IMongoCollection<TDocument>> mongoCollection,
      FilterDefinition<TDocument> filter,
      UpdateDefinition<TDocument> update,
      UpdateResult expectedResult)
   {
      var (docSerializer,serializer) = GetRendererForDocumentExpresion<TDocument>();

      mongoCollection.Setup(_ =>
            _.UpdateMany(It.Is<ExpressionFilterDefinition<TDocument>>(f =>
                  f.Render(docSerializer, serializer) == filter.Render(docSerializer, serializer)),
               It.Is<UpdateDefinition<TDocument>>(u =>
                  u.Render(docSerializer, serializer) == update.Render(docSerializer, serializer)),
               null,
               CancellationToken.None))
         .Returns(expectedResult);
   }

   public void WithTheDocumentsDeletedSuccessfullyInMongoDb<TDocument>(
      Mock<IMongoCollection<TDocument>> mongoCollection,
      FilterDefinition<TDocument> filter,
      DeleteResult expectedResult)
   {
      var (docSerializer,serializer) = GetRendererForDocumentExpresion<TDocument>();

      mongoCollection.Setup(_ =>
            _.DeleteMany(It.Is<ExpressionFilterDefinition<TDocument>>(f =>
                  f.Render(docSerializer, serializer) == filter.Render(docSerializer, serializer)),
               CancellationToken.None))
         .Returns(expectedResult);
   }

   public void ThanTheCollectionStoredTheItemsSuccessfullyAsynchronasly< TDocument>(Mock<IMongoCollection<TDocument>> collection,
      IEnumerable<TDocument> documents)
   {
      collection.Verify(_ => _.InsertManyAsync(documents,
            It.Is<InsertManyOptions>(o => o.IsOrdered == false), It.IsAny<CancellationToken>())
         , Times.Once);
   }

   public void ThanTheCollectionStoredTheItemsSuccessfully< TDocument>(Mock<IMongoCollection<TDocument>> collection,
      IEnumerable<TDocument> documents)
   {
      collection.Verify(_ => _.InsertMany(documents,
            It.Is<InsertManyOptions>(o => o.IsOrdered == false), It.IsAny<CancellationToken>())
         , Times.Once);
   }

   public void ThanTheCollectionStoredTheUnorderedItemsSuccessfully<TDocument>(Mock<IMongoCollection<TDocument>> collection,
      Expression<Func<IEnumerable<TDocument>, bool>> match)
   {
      collection.Verify(_ => _.InsertManyAsync(It.Is(match),
            It.Is<InsertManyOptions>(o => o.IsOrdered == false),
            It.IsAny<CancellationToken>())
         , Times.Once);
   }

   public (IBsonSerializer<TDocument>,IBsonSerializerRegistry) GetRendererForDocumentExpresion<TDocument>()
   {
      var serializerRegistry = BsonSerializer.SerializerRegistry;
      var documentSerializer = serializerRegistry.GetSerializer<TDocument>();
      return (documentSerializer, serializerRegistry);
   }

   public Mock<IAsyncCursor<AggregateCountResult>> GivenTheAggregateCountReturnsTheExpectesSet<T>(Mock<IMongoCollection<T>> collection, int countResult)
   {
      var countCursor = new Mock<IAsyncCursor<AggregateCountResult>>();

      collection.Setup(_ =>
            _.Aggregate(It.IsAny<AppendedStagePipelineDefinition<T, AggregateCountResult, AggregateCountResult>>(),
               It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
         .Returns((AppendedStagePipelineDefinition<T, AggregateCountResult, AggregateCountResult> e, AggregateOptions o,
            CancellationToken t) => countCursor.Object);

      countCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
         .Returns(true)
         .Returns(false);

      countCursor.Setup(_ => _.Current).Returns(new List<AggregateCountResult> { new AggregateCountResult(countResult) });

      return countCursor;
   }

   public Mock<IAsyncCursor<T>> GivenTheAggregateListReturnsTheExpectedSet<T>(Mock<IMongoCollection<T>> collection, IEnumerable<T> returnedData)
   {
      var cursor = new Mock<IAsyncCursor<T>>();

      collection.Setup(_ =>
            _.Aggregate(It.IsAny<AppendedStagePipelineDefinition<T,T,T>>(), It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
         .Returns((AppendedStagePipelineDefinition<T,T,T> e,AggregateOptions o,CancellationToken t) => cursor.Object);

      cursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
         .Returns(true)
         .Returns(false);

      cursor.Setup(_ => _.Current)
         .Returns(returnedData);

      return cursor;
   }

   public Mock<IAsyncCursor<T>> GivenTheAggregateListAsyncReturnsTheExpectedSet<T>(Mock<IMongoCollection<T>> collection, IEnumerable<T> returnedData)
   {
      var asyncCursor = new Mock<IAsyncCursor<T>>();

      collection.Setup(_ =>
            _.AggregateAsync(It.IsAny<AppendedStagePipelineDefinition<T,T,T>>(), It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync((AppendedStagePipelineDefinition<T,T,T> e,AggregateOptions o,CancellationToken t) => asyncCursor.Object);

      asyncCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(true)
         .ReturnsAsync(false);

      asyncCursor.Setup(_ => _.Current).Returns(returnedData);

      return asyncCursor;
   }
}
