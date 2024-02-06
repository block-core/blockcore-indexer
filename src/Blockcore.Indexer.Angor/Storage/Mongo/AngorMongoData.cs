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
            TotalInvestmentsCount = total,
            CreatedOnBlock = project.BlockIndex
         };
      }

      return null;
   }

   public async Task<ProjectStats?> GetProjectStatsAsync(string projectId)
   {
      var project = mongoDb.ProjectTable
         .AsQueryable()
         .FirstOrDefault(_ => _.AngorKey == projectId);

      if (project != null)
      {
         var total = await mongoDb.InvestmentTable.CountDocumentsAsync(Builders<Investment>.Filter.Eq(_ => _.AngorKey, project.AngorKey));

         var sum = mongoDb.InvestmentTable.AsQueryable()
            .Where(_ => _.AngorKey == projectId)
            .Sum(s => s.AmountSats);

         var test = mongoDb.InvestmentTable.AsQueryable()
            .Where(x => x.AngorKey == project.AngorKey)
            .GroupJoin(mongoDb.OutputTable.AsQueryable(),
               x => x.TransactionId,
               x => x.Outpoint.TransactionId,
               ((investment, outputs) => new { investment.AngorKey, investment.AmountSats, outputs }))
            .SelectMany(x => x.outputs, (firstLookup, table) =>
               new { firstLookup.AngorKey, firstLookup.AmountSats, table.Outpoint, })
            .GroupJoin(mongoDb.InputTable.AsQueryable(),
               output => output.Outpoint,
               input => input.Outpoint,
               (projection, inputs) => new { projection.AngorKey, projection.AmountSats, inputs })
            .Sum(x => x.inputs.Sum(i => i.Value));

         return new ProjectStats
         {
            InvestorCount = total,
            AmountInvested = sum,
            TotalAmountSpent = test
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
            TransactionId = _.TransactionId,
            TotalAmount = _.AmountSats,
            InvestorPublicKey = _.InvestorPubKey,
            HashOfSecret = _.SecretHash
         })
         .ToList();

      return new QueryResult<ProjectInvestment>
      {
         Items = investments,
         Offset = itemsToSkip,
         Limit = limit,
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
            TransactionId = _.TransactionId,
            InvestorPublicKey = _.InvestorPubKey,
            HashOfSecret = _.SecretHash
         })
         .FirstOrDefaultAsync();
   }
}
