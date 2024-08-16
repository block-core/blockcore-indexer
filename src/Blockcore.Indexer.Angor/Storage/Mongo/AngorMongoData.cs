using Blockcore.Indexer.Angor.Operations.Types;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
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
      var projectExists = mongoDb.ProjectTable
         .AsQueryable()
         .Any(_ => _.AngorKey == projectId);

      if (projectExists)
      {
         var total = await mongoDb.InvestmentTable.CountDocumentsAsync(Builders<Investment>.Filter.Eq(_ => _.AngorKey, projectId)).ConfigureAwait(false);

         var sum = mongoDb.InvestmentTable.AsQueryable()
            .Where(_ => _.AngorKey == projectId)
            .Sum(s => s.AmountSats);

         var spendingSummery = await GetSpentProjectFundsSplitToFounderAndPenalty(projectId).ConfigureAwait(false);

         return new ProjectStats
         {
            InvestorCount = total,
            AmountInvested = sum,
            AmountSpentSoFarByFounder = spendingSummery.founderSpent,
            AmountInPenalties = spendingSummery.investorWithdrawn,
            CountInPenalties = spendingSummery.invetorTrxCount
         };
      }

      return null;
   }


   private async Task<(long founderSpent, long investorWithdrawn, int invetorTrxCount)> GetSpentProjectFundsSplitToFounderAndPenalty(string projectId)
   {
      var founderSummery = "founder"; var investorSummery = "investor"; var total = "total"; var trxCount = "trxCount";

      var outputsSpentSummery = await mongoDb.InvestmentTable.Aggregate(PipelineDefinition<Investment, BsonDocument>.Create(
            //Filter by project id
            new BsonDocument("$match",
               new BsonDocument(nameof(Investment.AngorKey), projectId)),
            //Break down to object per stage outpoint for the inner join
            new BsonDocument("$unwind",
               new BsonDocument("path", "$" + nameof(Investment.StageOutpoint))),
            // Inner join to input table on outpoint for each stage
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Input" },
                  { "localField", nameof(Investment.StageOutpoint) },
                  { "foreignField", nameof(InputTable.Outpoint) },
                  { "as", "inputs" }
               }),
            new BsonDocument("$unwind", "$inputs"),
            //Aggregate the value by investment transaction and spending transaction and counting the number of trx in both
            new BsonDocument("$group",
               new BsonDocument
               {
                  { "_id",
                     new BsonDocument
                     {
                        { "TransactionId", "$" + nameof(Investment.TransactionId) },
                        { "TrxHash", "$inputs." + nameof(InputTable.TrxHash)}
                     } },
                  { "spent",
                     new BsonDocument("$sum", "$inputs." + nameof(InputTable.Value)) },
                  { "numTrx",
                     new BsonDocument("$sum", 1) }
               }),
            //Aggregate the value on 1 trx in both transactions or more than 1 in both transactions to identify founder and investor patterns
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
                           { "then", founderSummery },
                           { "else", investorSummery }
                        }) },
                  { total,
                     new BsonDocument("$sum", "$spent") },
                  { trxCount,
                     new BsonDocument("$sum", 1) }
               })))
         .ToListAsync();

      var founder = outputsSpentSummery.SingleOrDefault(x => x["_id"] == founderSummery);

      var investor = outputsSpentSummery.SingleOrDefault(x => x["_id"] == investorSummery);

      return (founder?[total].AsInt64 ?? 0, investor?[total].AsInt64 ?? 0, investor?[trxCount].AsInt32 ?? 0);
   }

   //Not used but kept for now for reference
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

   /// <summary>
   /// Sum of all inputs spending outputs from the investment transaction
   /// </summary>
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
         .OrderBy(x => x.BlockIndex)
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
         }).OrderByDescending(x => x.CreatedOnBlock),
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
         .OrderBy(x => x.BlockIndex)
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
