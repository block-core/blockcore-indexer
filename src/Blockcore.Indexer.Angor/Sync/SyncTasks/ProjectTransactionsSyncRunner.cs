using System.Linq.Expressions;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
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
      var pipeline = PipelineDefinition<Investment, BsonDocument>.Create(new[]
      {
         new BsonDocument("$match",
            new BsonDocument("$expr",
               new BsonDocument("$eq", new BsonArray
               {
                  "$AngorKey", // Replace 'foreignField' with the actual field name
                  "$$angorProjectId"
               })))
      ,
      new BsonDocument($"$group",new BsonDocument
      {
         { "_id", "$AngorKey" },
         { "projectMaxBlockScanned", new BsonDocument("$max", "$BlockIndex") }
      }),
      new BsonDocument("$project",new BsonDocument("projectMaxBlockScanned", 1))
      });


      var matchPipeline = new BsonDocument("$match",
         new BsonDocument("$expr",
            new BsonDocument("$eq", new BsonArray
            {
               "$TransactionId", // Replace 'foreignField' with the actual field name
               "$outputs.Outpoint.TransactionId"
            })));

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
                     { "AddressOnFeeOutput", 1 }, { "TransactionId", 1 }, { "joinedData.projectMaxBlockScanned", 1 }
                  }),
               new BsonDocument("$lookup",
                  new BsonDocument
                  {
                     { "from", "Output" },
                     { "localField", "AddressOnFeeOutput" },
                     { "foreignField", "Address" },
                     { "as", "o" }
                  }),
               new BsonDocument("$unwind", "$o"),
               new BsonDocument("$match",
                  new BsonDocument("$expr",
                     new BsonDocument("$and", new BsonArray
                     {
                        new BsonDocument("$ne",
                           new BsonArray { "$TransactionId", "$o.Outpoint.TransactionId" }),
                        new BsonDocument("$gt",
                           new BsonArray { "$o.BlockIndex", "$joinedData.projectMaxBlockScanned" })
                     }))),
               new BsonDocument("$project",
                  new BsonDocument
                  {
                     { "OutputTransactionId", "$o.Outpoint.TransactionId" },
                     { "OutputBlockIndex", "$o.BlockIndex" }
                  })
            }))


         .ToListAsync();

      // var investmentsInProjectOutputs = await angorMongoDb.ProjectTable.Aggregate()
      //    .Lookup(angorMongoDb.InvestmentTable,
      //       new BsonDocument("angorProjectId", "_id"),
      //       pipeline,
      //       new StringFieldDefinition<BsonDocument, List<BsonDocument>>("joinedData"))
      //    .Unwind("joinedData",new AggregateUnwindOptions<BsonDocument>{PreserveNullAndEmptyArrays = true})
      //    .Project(Builders<BsonDocument>.Projection
      //       .Include("AddressOnFeeOutput")
      //       .Include("TransactionId")
      //       .Include("joinedData.projectMaxBlockScanned")
      //    )
      //    .Lookup(angorMongoDb.OutputTable.CollectionNamespace.CollectionName,
      //       new StringFieldDefinition<BsonDocument>("AddressOnFeeOutput"),
      //       new StringFieldDefinition<OutputTable>("Address"),
      //       new StringFieldDefinition<OutputTable>("outputs"))
      //    .Unwind("outputs")
      //    .Match(Builders<BsonDocument>.Filter.Ne("TransactionId", "$outputs.Outpoint.TransactionId"))
      // .Project(new BsonDocument
      // {
      //    { "OutputTransactionId", "$outputs.Outpoint.TransactionId" },
      //    { "OutputBlockIndex", "$outputs.BlockIndex" }
      // })
      //    .ToListAsync();

      var investments = new List<Investment>();

      foreach (var investmentOutput in investmentsInProjectOutputs)
      {
         var allOutputsOnInvestmentTransaction = await angorMongoDb.OutputTable.AsQueryable()
            .Where(output => output.BlockIndex == investmentOutput["OutputBlockIndex"] &&
                             output.Outpoint.TransactionId == investmentOutput["OutputTransactionId"])
            .ToListAsync();

         if (allOutputsOnInvestmentTransaction.All(x => x.Address != "none")) //TODO replace with a better indicator of stage investments
            continue;

         var feeOutput = allOutputsOnInvestmentTransaction.Single(x => x.Outpoint.OutputIndex == 0);
         var projectDataOutput = allOutputsOnInvestmentTransaction.Single(x => x.Outpoint.OutputIndex == 1);

         var projectInfoScript = Script.FromHex(projectDataOutput.ScriptHex);

         var investorPubKey = Encoders.Hex.EncodeData(projectInfoScript.ToOps()[1].PushData);

         if (investments.Any(_ => _.InvestorPubKey == investorPubKey) ||
             angorMongoDb.InvestmentTable.AsQueryable().Any(_ => _.InvestorPubKey == investorPubKey)) //Investor key is the _id of that document
         {
            logger.LogInformation($"Multiple transactions with the same investor public key {investorPubKey} for the same project {feeOutput.ScriptHex}");
            continue;
         }

         var project = await angorMongoDb.ProjectTable.Aggregate() //TODO not sure we need this as we get the lookup from the project table to begine with
            .Match(_ => _.AngorKeyScriptHex == feeOutput.ScriptHex)
            .SingleOrDefaultAsync();

         if (project == null)
            continue;

         var hashOfSecret = projectInfoScript.ToOps().Count == 3
            ? Encoders.Hex.EncodeData(projectInfoScript.ToOps()[2].PushData)
            : string.Empty;

         var investment = new Investment
         {
            InvestorPubKey = investorPubKey,
            AngorKey = project.AngorKey,
            AmountSats = allOutputsOnInvestmentTransaction.Where(_ => _.Address == "none").Sum(_ => _.Value),
            BlockIndex = feeOutput.BlockIndex,
            SecretHash = hashOfSecret,
            TransactionId = feeOutput.Outpoint.TransactionId,
         };

         investments.Add(investment);
      }

      if (!investments.Any())
         return false;

      await angorMongoDb.InvestmentTable.InsertManyAsync(investments);

      return true;

   }
}
