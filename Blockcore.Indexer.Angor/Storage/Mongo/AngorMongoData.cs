using Blockcore.Indexer.Angor.Operations.Types;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Sync;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Angor.Storage.Mongo;

public class AngorMongoData : MongoData, IAngorStorage
{
   readonly IAngorMongoDb mongoDb;

   public AngorMongoData(ILogger<AngorMongoDb> dbLogger, SyncConnection connection, IOptions<ChainSettings> chainConfiguration,
      GlobalState globalState, IMapMongoBlockToStorageBlock mongoBlockToStorageBlock, ICryptoClientFactory clientFactory,
      IScriptInterpeter scriptInterpeter, IMongoDatabase mongoDatabase, IMongoDb db, IBlockRewindOperation rewindOperation,
      IComputeHistoryQueue computeHistoryQueue, IAngorMongoDb mongoDb)
      : base(dbLogger, connection, chainConfiguration, globalState, mongoBlockToStorageBlock, clientFactory,
         scriptInterpeter, mongoDatabase, db, rewindOperation, computeHistoryQueue)
   {
      this.mongoDb = mongoDb;
   }


   public async Task<ProjectIndexerData?> GetProjectAsync(string projectId)
   {
      var project = mongoDb.ProjectTable
         .AsQueryable()
         .FirstOrDefault(_ => _.AngorKey == projectId);

      if (project != null)
      {
         return new ProjectIndexerData
         {
            FounderKey = project.FounderKey, ProjectIdentifier = project.AngorKey, TrxHex = project.TransactionId
         };
      }

      return null;
   }

   public async Task<QueryResult<ProjectIndexerData>> GetProjectsAsync(int? offset, int limit)
   {
      long total = await mongoDb.ProjectTable.CountDocumentsAsync(FilterDefinition<Project>.Empty);

      int itemsToSkip = offset ?? ((int)total < limit ? 0 : (int)total - limit);

      var projects = mongoDb.ProjectTable.AsQueryable()
         .Skip(itemsToSkip)
         .Take(limit)
         .ToList();

      return new QueryResult<ProjectIndexerData>
      {
         Items = projects.Select(_ => new ProjectIndexerData
         {
            FounderKey = _.FounderKey,ProjectIdentifier = _.AngorKey,TrxHex = _.TransactionId
         }),
         Offset = itemsToSkip,
         Limit = limit,
         Total = total
      };
   }
}
