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
      IScriptInterpreter scriptInterpeter, IMongoDatabase mongoDatabase, IMongoDb db, IBlockRewindOperation rewindOperation,
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

      var total = project == null ? (long?)null : await mongoDb.InvestmentTable.CountDocumentsAsync(Builders<Investment>.Filter.Eq(_ => _.AngorKey,project.AngorKey));

      if (project != null)
      {
         return new ProjectIndexerData
         {
            FounderKey = project.FounderKey,
            NostrPubKey = project.NPubKey,
            ProjectIdentifier = project.AngorKey,
            TrxId = project.TransactionId,
            TotalInvestmentsCount = total
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
            FounderKey = _.FounderKey,
            NostrPubKey =_.NPubKey,
            ProjectIdentifier = _.AngorKey,
            TrxId = _.TransactionId,
            CreatedOnBlock = _.BlockIndex
         }),
         Offset = itemsToSkip,
         Limit = limit,
         Total = total
      };
   }

   public async Task<QueryResult<ProjectInvestment>> GetProjectInvestmentsAsync(string projectId, int? offset, int limit)
   {
      long total = await mongoDb.InvestmentTable.CountDocumentsAsync(Builders<Investment>.Filter.Eq(_ => _.AngorKey,projectId));

      int itemsToSkip = offset ?? ((int)total < limit ? 0 : (int)total - limit);

      var investments = mongoDb.InvestmentTable.AsQueryable()
         .Where(_ => _.AngorKey == projectId)
         .Skip(itemsToSkip)
         .Take(limit)
         .Select(_ => new ProjectInvestment
         {
            TransactionId = _.TransactionIndex,
            TotalAmount = _.AmountSats,
            InvestorPublicKey = _.InvestorPubKey,
            HashOfSecret = _.SecretHash
         })
         .ToList();

      return new QueryResult<ProjectInvestment>
      {
         Items = investments,
         Offset = 0,
         Limit = 0,
         Total = total
      };
   }

   public Task<ProjectInvestment> GetInvestmentsByInvestorPubKeyAsync(string investorPubKey)
   {
      return mongoDb.InvestmentTable.Aggregate()
         .Match(i => i.InvestorPubKey == investorPubKey)
         .Project(_ => new ProjectInvestment
         {
            TotalAmount = _.AmountSats,
            TransactionId = _.TransactionIndex,
            InvestorPublicKey = _.InvestorPubKey,
            HashOfSecret = _.SecretHash
         })
         .FirstOrDefaultAsync();
   }
}
