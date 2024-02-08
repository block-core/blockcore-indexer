using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Blockcore.NBitcoin.DataEncoders;
using Microsoft.Extensions.Options;
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
      var investmentsInProjectOutputs = await angorMongoDb.ProjectTable.AsQueryable()
         .GroupJoin(
            angorMongoDb.OutputTable.AsQueryable(),
            p => p.AddressOnFeeOutput,
            o => o.Address,
            (project, outputs) => new { project.TransactionId, project.BlockIndex, outputs })
         .SelectMany(
            p => p.outputs.Where(
               o => o.BlockIndex > p.BlockIndex && p.TransactionId != o.Outpoint.TransactionId),
            (p, o) => new { outputTransactionId = o.Outpoint.TransactionId, outoutBlockIndex = o.BlockIndex })
         .GroupJoin(
            angorMongoDb.InvestmentTable.AsQueryable(),
            x => x.outputTransactionId,
            i => i.TransactionId,
            (data, investments) => new { data.outputTransactionId, data.outoutBlockIndex, investments })
         .Where(data => !data.investments.Any())
         .ToListAsync();

      var investments = new List<Investment>();

      foreach (var investmentOutput in investmentsInProjectOutputs)
      {
         var allOutputsOnInvestmentTransaction = await angorMongoDb.OutputTable.AsQueryable()
            .Where(output => output.BlockIndex == investmentOutput.outoutBlockIndex &&
                             output.Outpoint.TransactionId == investmentOutput.outputTransactionId)
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
