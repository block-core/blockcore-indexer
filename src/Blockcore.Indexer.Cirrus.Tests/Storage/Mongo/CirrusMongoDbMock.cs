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
   public Mock<IMongoCollection<CirrusContractCodeTable>> CirrusContractCodeTableCollection;
   public Mock<IMongoCollection<DaoContractComputedTable>> DaoContractComputedTableCollection;

   private Mock<ICirrusMongoDb> cirrusDb;

   public CirrusMongoDbMock()
   : base()
   {
      CirrusContractTableCollection = new Mock<IMongoCollection<CirrusContractTable>>();
      CirrusContractCodeTableCollection = new Mock<IMongoCollection<CirrusContractCodeTable>>();
      DaoContractComputedTableCollection = new Mock<IMongoCollection<DaoContractComputedTable>>();

      cirrusDb = new Mock<ICirrusMongoDb>();

      cirrusDb.Setup(_ => _.CirrusContractTable).Returns(CirrusContractTableCollection.Object);
      cirrusDb.Setup(_ => _.CirrusContractCodeTable).Returns(CirrusContractCodeTableCollection.Object);
      cirrusDb.Setup(_ => _.DaoContractComputedTable).Returns(DaoContractComputedTableCollection.Object);
   }

   public ICirrusMongoDb CirrusMongoDbObject => cirrusDb.Object;

   private void SetupAsQueryable<T>(Mock<IMongoCollection<T>> doc)
      => doc.Setup(_ => _.AsQueryable(null))
         .Returns(new Mock<IMongoQueryable<T>>().Object);
}
