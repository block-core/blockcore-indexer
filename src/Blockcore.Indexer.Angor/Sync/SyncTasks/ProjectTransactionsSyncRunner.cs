using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Blockcore.NBitcoin.DataEncoders;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using static System.String;

namespace Blockcore.Indexer.Angor.Sync.SyncTasks;

public class ProjectInvestmentsSyncRunner : TaskRunner
{
   readonly IAngorMongoDb angorMongoDb;
   ILogger<ProjectInvestmentsSyncRunner> logger;

   public ProjectInvestmentsSyncRunner(IOptions<IndexerSettings> configuration,  ILogger<ProjectInvestmentsSyncRunner> logger,
      IAngorMongoDb angorMongoDb)
      : base(configuration, logger)
   {
      this.angorMongoDb = angorMongoDb;
      this.logger = logger;
      Delay = TimeSpan.FromMinutes(1);
   }

   private bool CanRunInvestmentsSync()
   {
      return !( //sync with other runners
         !Runner.GlobalState.IndexModeCompleted ||
         Runner.GlobalState.Blocked ||
         Runner.GlobalState.ReorgMode ||
         Runner.GlobalState.StoreTip == null ||
         Runner.GlobalState.IndexMode);
   }

   public override async Task<bool> OnExecute()
   {
      if (!CanRunInvestmentsSync())
         return false;

      var investmentsInProjectOutputs = await angorMongoDb.ProjectTable
         .Aggregate(PipelineDefinition<Project, BsonDocument>
            .Create(MongoDbLookupForInvestments()))
         .ToListAsync();

      var investmentTasks = investmentsInProjectOutputs.Select(ValidateAndCreateInvestmentAsync).ToList();

      await Task.WhenAll(investmentTasks);

      var investments = investmentTasks.AsEnumerable()
         .Where(x => x is { Result: not null, IsCompletedSuccessfully: true })
         .Select(x => x.Result)
         .OrderBy(x => x!.BlockIndex)
         .ToList();

      if (investments.Count == 0)
         return false;

      await angorMongoDb.InvestmentTable.InsertManyAsync(investments, new InsertManyOptions { IsOrdered = true })
         .ConfigureAwait(false);

      return true;

   }

   const string ErrorMssage = "Multiple transactions with the same investor public key {0} for the same project {1}";
   async Task<Investment?> ValidateAndCreateInvestmentAsync(BsonDocument investmentOutput)
   {
      var feeOutput = await angorMongoDb.OutputTable.AsQueryable().SingleAsync(x =>
         x.Outpoint == new Outpoint{ TransactionId = investmentOutput["OutputTransactionId"].ToString(), OutputIndex = 0});

      var projectDataOutput = await angorMongoDb.OutputTable.AsQueryable().SingleAsync(x =>
         x.Outpoint == new Outpoint{ TransactionId = investmentOutput["OutputTransactionId"].ToString(), OutputIndex = 1});

      var projectInfoScript = Script.FromHex(projectDataOutput.ScriptHex);

      var investorPubKey = Encoders.Hex.EncodeData(projectInfoScript.ToOps()[1].PushData);

      if (angorMongoDb.InvestmentTable.AsQueryable()
             .Any(_ => _.InvestorPubKey == investorPubKey)) //Investor key is the _id of that document
      {
         logger.LogDebug(ErrorMssage,investorPubKey,feeOutput.ScriptHex);
         return null;
      }

      var project = await angorMongoDb.ProjectTable
         .Aggregate() //TODO not sure we need this as we get the lookup from the project table to begin with
         .Match(_ => _.AngorKeyScriptHex == feeOutput.ScriptHex)
         .SingleOrDefaultAsync();

      if (project == null)
         return null;

      var hashOfSecret = projectInfoScript.ToOps().Count == 3
         ? Encoders.Hex.EncodeData(projectInfoScript.ToOps()[2].PushData)
         : Empty;

      int outpointIndex = 2;
      OutputTable? stage;
      List<OutputTable> stages = new();
      do
      {
         stage = await angorMongoDb.OutputTable.AsQueryable()
            .Where(output => output.Outpoint == new Outpoint{
               TransactionId = investmentOutput["OutputTransactionId"].ToString(), OutputIndex = outpointIndex} &&
                             output.Address == "none")
            .SingleOrDefaultAsync();

         outpointIndex += 1;

         if (stage != null)
            stages.Add(stage);

      } while (stage != null);

      if (stages.Count == 0)
         return null;

      return new Investment
      {
         InvestorPubKey = investorPubKey,
         AngorKey = project.AngorKey,
         AmountSats = stages.Sum(_ => _.Value),
         BlockIndex = feeOutput.BlockIndex,
         SecretHash = hashOfSecret,
         TransactionId = feeOutput.Outpoint.TransactionId,
         StageOutpoint = stages.Select(x => x.Outpoint).ToList()
      };
   }

   private BsonDocument[] MongoDbLookupForInvestments()
   {
      return new[]
      {
         //Left join to investment table on Angor key get max block index to only look an new blocks
         new BsonDocument("$lookup",
            new BsonDocument
            {
               { "from", "Investment" },
               { "let", new BsonDocument("angorProjectId", "_id") },
               {
                  "pipeline", new BsonArray
                  {
                     new BsonDocument("$match",
                        new BsonDocument("$expr",
                           new BsonDocument("$eq",
                              new BsonArray { "$AngorKey", "$$angorProjectId" }))),
                     new BsonDocument("$group",
                        new BsonDocument
                        {
                           { "_id", "$AngorKey" },
                           { "projectMaxBlockScanned", new BsonDocument("$max", "$BlockIndex") }
                        }),
                     new BsonDocument("$project",
                        new BsonDocument("projectMaxBlockScanned", 1))
                  }
               },
               { "as", "joinedData" }
            }),
         new BsonDocument("$unwind",
            new BsonDocument { { "path", "$joinedData" }, { "preserveNullAndEmptyArrays", true } }),
         //Only take address transaction id and max block
         new BsonDocument("$project",
            new BsonDocument
            {
               { "AddressOnFeeOutput", 1 },
               { "TransactionId", 1 },
               {
                  "projectMaxBlockScanned",
                  new BsonDocument("$ifNull", new BsonArray { "$joinedData.projectMaxBlockScanned", $"$BlockIndex" })
               }
            }),
         //Inner join with output on the indexed address and greater than block index and filter by trensaction id
         new BsonDocument("$lookup",
            new BsonDocument
            {
               { "from", "Output" },
               {
                  "let",
                  new BsonDocument
                  {
                     { "address", "$AddressOnFeeOutput" },
                     { "projectMaxBlockScanned", "$projectMaxBlockScanned" }
                  }
               },
               {
                  "pipeline", new BsonArray
                  {
                     new BsonDocument("$match",
                        new BsonDocument("$expr",
                           new BsonDocument("$eq",
                              new BsonArray { "$Address", "$$address" }))),
                     new BsonDocument("$match",
                        new BsonDocument("$expr",
                           new BsonDocument("$gt",
                              new BsonArray { "$BlockIndex", "$$projectMaxBlockScanned" }))),
                     new BsonDocument("$match",
                        new BsonDocument("$expr",
                           new BsonDocument("$eq",
                              new BsonArray { "$Outpoint.OutputIndex", 0 }))),
                  }
               },
               { "as", "o" }
            }),
         new BsonDocument("$unwind", "$o"),
         // Only take fields needed for performance
         new BsonDocument("$project",
            new BsonDocument
            {
               { "OutputTransactionId", "$o.Outpoint.TransactionId" }, { "OutputBlockIndex", "$o.BlockIndex" }
            })
      };
   }
}
