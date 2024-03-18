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
using MongoDB.Bson;
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

         // var totalInvestmentWithdrawnByInvestors = await GetTotalInvestmentWithdrawn(projectId);
         //
         // var totalOutputsUsed = await GetSumOfOutputsSpentOnProject(projectId);

         var outputsSpentSummery = await GetSpentProjectFundsSplitToFounderAndPenalty(projectId);

         var founder = outputsSpentSummery.SingleOrDefault(x => x["_id"] == "founder");

         var investor = outputsSpentSummery.SingleOrDefault(x => x["_id"] == "investor");

         return new ProjectStats
         {
            InvestorCount = total,
            AmountInvested = sum,
            AmountSpentSoFarByFounder = founder?["total"].AsInt64 ?? 0,
            AmountInPenalties = investor?["total"].AsInt64 ?? 0,
            CountInPenalties = investor?["trxCount"].AsInt32 ?? 0
         };
      }

      return null;
   }


   private Task<List<BsonDocument>> GetSpentProjectFundsSplitToFounderAndPenalty(string projectId)
   {
      return mongoDb.InvestmentTable.Aggregate(PipelineDefinition<Investment, BsonDocument>.Create(
            new BsonDocument("$match",
               new BsonDocument("AngorKey", projectId)),
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Input" },
                  { "localField", "TransactionId" },
                  { "foreignField", "Outpoint.TransactionId" },
                  { "as", "inputs" }
               }),
            new BsonDocument("$unwind", "$inputs"),
            new BsonDocument("$match",
               new BsonDocument("inputs.Address", "none")), //Remove change address
            new BsonDocument("$group",
               new BsonDocument
               {
                  { "_id",
                     new BsonDocument
                     {
                        { "TransactionId", "$TransactionId" },
                        { "TrxHash", "$inputs.TrxHash" }
                     } },
                  { "spent",
                     new BsonDocument("$sum", "$inputs.Value") },
                  { "numTrx",
                     new BsonDocument("$sum", 1) }
               }),
            new BsonDocument("$group",
               new BsonDocument
               {
                  { "_id",
                     new BsonDocument("$cond",
                        new BsonDocument
                        {
                           { "if",
                              new BsonDocument("$eq",
                                 new BsonArray
                                 {
                                    "$numTrx",
                                    1
                                 }) },
                           { "then", "founder" },
                           { "else", "investor" }
                        }) },
                  { "total",
                     new BsonDocument("$sum", "$spent") },
                  { "trxCount",
                     new BsonDocument("$sum", 1) }
               })))
         .ToListAsync();
   }

   private Task<BsonDocument> GetTotalInvestmentWithdrawn(string projectId)
   {
      return mongoDb.InvestmentTable.Aggregate(PipelineDefinition<Investment, BsonDocument>.Create(new[]
         {
            new BsonDocument("$match",
               new BsonDocument("AngorKey", projectId)),
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Input" },
                  { "localField", "TransactionId" },
                  { "foreignField", "Outpoint.TransactionId" },
                  { "as", "joinedData" }
               }),
            new BsonDocument("$unwind", "$joinedData"),
            new BsonDocument("$group",
               new BsonDocument
               {
                  {
                     "_id", new BsonDocument { { "AngorKey", "$AngorKey" }, { "TransactionId", "$joinedData.TrxHash" } }
                  },
                  { "count", new BsonDocument("$sum", 1) },
                  { "totalValue", new BsonDocument("$sum", "$joinedData.Value") }
               }),
            new BsonDocument("$match",
               new BsonDocument("count",
                  new BsonDocument("$gt", 1))),
            new BsonDocument("$group",
               new BsonDocument
               {
                  { "_id", "$_id.AngorKey" }, { "totalValueSum", new BsonDocument("$sum", "$totalValue") }
               }),
            new BsonDocument("$project",
               new BsonDocument { { "_id", 0 }, { "AngorKey", "$_id" }, { "totalValueSum", 1 } })
         }))
         .FirstOrDefaultAsync();
   }

   private Task<BsonDocument> GetSumOfOutputsSpentOnProject(string projectId)
   {
      return mongoDb.InvestmentTable.Aggregate(PipelineDefinition<Investment, BsonDocument>.Create(new[]
      {
         new BsonDocument("$match",
            new BsonDocument("AngorKey", projectId)),
         new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Input" },
                  { "localField", "TransactionId" },
                  { "foreignField", "Outpoint.TransactionId" },
                  { "as", "inputs" }
               }),
            new BsonDocument("$unwind",
               new BsonDocument("path", "$inputs")),
            new BsonDocument("$group",
               new BsonDocument
               {
                  { "_id", "$AngorKey" },
                  { "Spent",
                     new BsonDocument("$sum", "$inputs.Value") }
               })
         })).FirstOrDefaultAsync();
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
