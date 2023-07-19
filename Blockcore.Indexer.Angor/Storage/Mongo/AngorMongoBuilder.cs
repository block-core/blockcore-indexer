using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Angor.Storage.Mongo;

public class AngorMongoBuilder : MongoBuilder
{
   public IAngorMongoDb AngorMongoDb { get; set; }

   public AngorMongoBuilder(ILogger<AngorMongoBuilder> logger, IMongoDb data, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainSettings)
      : base(logger, data, nakoConfiguration, chainSettings)
   { }

   public override Task OnExecute()
   {
      base.OnExecute();

      if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Project)))
      {
         MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Project>(_ =>
         {
            _.AutoMap();
            _.MapIdProperty(_ => _.AngorKey);
         });
      }

      if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(Investment)))
      {
         MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<Investment>(_ =>
         {
            _.AutoMap();
            _.MapIdProperty(_ => _.InvestorPubKey);
         });
      }

      //TODO move this to the block indexer task runner, but we'll need to move the indexes in there to a different class for each project/blockchain
      AngorMongoDb.ProjectTable.Indexes
         .CreateOne(new CreateIndexModel<Project>(Builders<Project>
            .IndexKeys.Hashed(_ => _.FounderKey)));

      //TODO move this to the block indexer task runner, but we'll need to move the indexes in there to a different class for each project/blockchain
      AngorMongoDb.InvestmentTable.Indexes
         .CreateOne(new CreateIndexModel<Investment>(Builders<Investment>
            .IndexKeys.Hashed(_ => _.AngorKey)));

      return Task.CompletedTask;
   }
}
