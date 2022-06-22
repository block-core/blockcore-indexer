using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Tests.Storage.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Moq;

namespace Blockcore.Indexer.Cirrus.Tests.Storage.Mongo;

public class CirrusMongoDbMock : MongodbMock
{
   public Mock<IMongoCollection<CirrusContractTable>> CirrusContractTableCollection;
   public Mock<IMongoQueryable<CirrusContractTable>> CirrusContractTableQuariable;
   public Mock<IMongoCollection<CirrusContractCodeTable>> CirrusContractCodeTableCollection;
   public Mock<IMongoQueryable<CirrusContractCodeTable>> CirrusContractCodeTableQuariable;
   public Mock<IMongoCollection<DaoContractTable>> DaoContractComputedTableCollection;
   public Mock<IMongoQueryable<DaoContractTable>> DaoContractComputedTableQuariable;

   private Mock<ICirrusMongoDb> cirrusDb;

   public CirrusMongoDbMock()
   : base()
   {
      CirrusContractTableCollection = new Mock<IMongoCollection<CirrusContractTable>>();
      CirrusContractTableQuariable = new Mock<IMongoQueryable<CirrusContractTable>>();
      CirrusContractCodeTableCollection = new Mock<IMongoCollection<CirrusContractCodeTable>>();
      DaoContractComputedTableCollection = new Mock<IMongoCollection<DaoContractTable>>();

      cirrusDb = new Mock<ICirrusMongoDb>();

      cirrusDb.Setup(_ => _.CirrusContractTable).Returns(CirrusContractTableCollection.Object);
      cirrusDb.Setup(_ => _.CirrusContractCodeTable).Returns(CirrusContractCodeTableCollection.Object);
      cirrusDb.Setup(_ => _.DaoContractComputedTable).Returns(DaoContractComputedTableCollection.Object);

      cirrusDb.Setup(_ => _.DaoContractComputedTable.Database)
        .Returns(MongoDatabaseObject);
      
     DaoContractComputedTableCollection.Setup(_ => _.Settings)
        .Returns(new MongoCollectionSettings());

     var client = new Mock<IMongoClient>();

     mongodatabase.Setup(_ => _.Client).Returns(client.Object);

     client.Setup(_ => _.Settings).Returns(new MongoClientSettings());

   }

   public ICirrusMongoDb CirrusMongoDbObject => cirrusDb.Object;

}
