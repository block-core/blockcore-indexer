using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
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

   private Mock<IMongoDatabase> mongodatabase;

   public  MongodbMock()
   {
      blockTableCollection = new Mock<IMongoCollection<BlockTable>>();
      transactionBlockTableCollection = new Mock<IMongoCollection<TransactionBlockTable>>();
      outputTableCollection = new Mock<IMongoCollection<OutputTable>>();
      unspentOutputTableCollection = new Mock<IMongoCollection<UnspentOutputTable>>();
      inputTableCollection = new Mock<IMongoCollection<InputTable>>();
      transactionTable = new Mock<IMongoCollection<TransactionTable>>();

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

      mongodatabase.Setup(_ => _.Client)
         .Returns(new Mock<IMongoClient>().Object);
   }

   public IMongoDatabase Object => mongodatabase.Object;

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

   public void ThanTheCollectionStoredTheItemsSuccessfully< TDocument>(Mock<IMongoCollection<TDocument>> collection,
      IEnumerable<TDocument> documents)
   {
      collection.Verify(_ => _.InsertManyAsync(documents,
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
}
