using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Blockcore.NBitcoin.DataEncoders;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

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
   }


   public override async Task<bool> OnExecute()
   {
      var investmentsInProjectOutputs = await angorMongoDb.ProjectTable.Aggregate(PipelineDefinition<Project, BsonDocument>.Create(
            new[]
            {
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
               new BsonDocument("$project",
                  new BsonDocument
                  {
                     { "AddressOnFeeOutput", 1 }, { "TransactionId", 1 }, { "projectMaxBlockScanned", new BsonDocument("$ifNull", new BsonArray { "$joinedData.projectMaxBlockScanned", 0 }) }
                  }),
               new BsonDocument("$lookup",
                  new BsonDocument
                  {
                     { "from", "Output" },
                     { "let", new BsonDocument{{"address" , "$AddressOnFeeOutput" },{"trx", "$TransactionId"},{"projectMaxBlockScanned", "$projectMaxBlockScanned"}}},
                     { "pipeline", new BsonArray
                     {
                        new BsonDocument("$match",
                           new BsonDocument("$expr",
                              new BsonDocument("$eq",
                                 new BsonArray { "$Address", "$$address" }))),
                        new BsonDocument("$match",
                           new BsonDocument("$expr",
                              new BsonDocument("$and", new BsonArray
                              {
                                 new BsonDocument("$gt",
                                    new BsonArray { "$BlockIndex","$$projectMaxBlockScanned"}),
                                 new BsonDocument("$ne",
                                    new BsonArray { "$$trx","$Outpoint.TransactionId"})
                              })))
                     } },
                     { "as", "o" }
                  }),
               new BsonDocument("$unwind", "$o"),
               new BsonDocument("$project",
                  new BsonDocument
                  {
                     { "OutputTransactionId", "$o.Outpoint.TransactionId" },
                     { "OutputBlockIndex", "$o.BlockIndex" }
                  })
            }))
         .ToListAsync();

      var investmentTasks = investmentsInProjectOutputs.Select(ValidateAndCreateInvestmentAsync).ToList();

      await Task.WhenAll(investmentTasks);

      var investments = investmentTasks.AsEnumerable()
         .Select(x => x.Result)
         .Where(x => x != null)
         .OrderBy(x => x!.BlockIndex)
         .Distinct()
         .ToList();

      if (!investments.Any())
         return false;

      await angorMongoDb.InvestmentTable.InsertManyAsync(investments);

      return true;

   }

   async Task<Investment?> ValidateAndCreateInvestmentAsync(BsonDocument investmentOutput)
   {
      var allOutputsOnInvestmentTransaction = await angorMongoDb.OutputTable.AsQueryable()
         .Where(output => output.BlockIndex == investmentOutput["OutputBlockIndex"] &&
                          output.Outpoint.TransactionId == investmentOutput["OutputTransactionId"])
         .ToListAsync();

      if (allOutputsOnInvestmentTransaction.All(x =>
             x.Address != "none")) //TODO replace with a better indicator of stage investments
         return null;

      var feeOutput = allOutputsOnInvestmentTransaction.Single(x => x.Outpoint.OutputIndex == 0);
      var projectDataOutput = allOutputsOnInvestmentTransaction.Single(x => x.Outpoint.OutputIndex == 1);

      var projectInfoScript = Script.FromHex(projectDataOutput.ScriptHex);

      var investorPubKey = Encoders.Hex.EncodeData(projectInfoScript.ToOps()[1].PushData);

      if (angorMongoDb.InvestmentTable.AsQueryable()
             .Any(_ => _.InvestorPubKey == investorPubKey)) //Investor key is the _id of that document
      {
         logger.LogInformation(
            $"Multiple transactions with the same investor public key {investorPubKey} for the same project {feeOutput.ScriptHex}");
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
         : string.Empty;

      return new Investment
      {
         InvestorPubKey = investorPubKey,
         AngorKey = project.AngorKey,
         AmountSats = allOutputsOnInvestmentTransaction.Where(_ => _.Address == "none").Sum(_ => _.Value),
         BlockIndex = feeOutput.BlockIndex,
         SecretHash = hashOfSecret,
         TransactionId = feeOutput.Outpoint.TransactionId,
      };
   }
}
