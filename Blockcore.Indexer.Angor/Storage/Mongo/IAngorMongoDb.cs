using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo;
using MongoDB.Driver;

namespace Blockcore.Indexer.Angor.Storage.Mongo;

public interface IAngorMongoDb : IMongoDb
{
   IMongoCollection<Project> ProjectTable { get; }
   IMongoCollection<Investment> InvestmentTable { get; }
}
