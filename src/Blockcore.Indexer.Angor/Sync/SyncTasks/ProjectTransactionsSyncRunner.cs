using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Angor.Sync.SyncTasks;

public class ProjectTransactionsSyncRunner : TaskRunner
{
   readonly IAngorMongoDb angorMongoDb;
   ILogger<ProjectTransactionsSyncRunner> logger;

   public ProjectTransactionsSyncRunner(IOptions<IndexerSettings> configuration,  ILogger<ProjectTransactionsSyncRunner> logger,
      IAngorMongoDb angorMongoDb)
      : base(configuration, logger)
   {
      this.angorMongoDb = angorMongoDb;
      this.logger = logger;
   }


   public override async Task<bool> OnExecute()
   {
      var testTrx = await angorMongoDb.ProjectTable.AsQueryable()
         .GroupJoin(
            angorMongoDb.OutputTable.AsQueryable(),
            p => p.AngorKey,
            o => o.Address,
            (project, outputs) => new { project, outputs })
         .Where(_ => _.outputs.Count() > 3)
         .SelectMany(projection => projection.outputs,
            (p, o) => new Investment
            {
               AngorKey = p.project.AngorKey,
               AmountSats = p.outputs.Where(_ => _.Address != p.project.AngorKey).Sum(_ => _.Value),
               BlockIndex = o.BlockIndex,
               SecretHash = o.ScriptHex.Substring(32),
               TransactionIndex = o.Outpoint.TransactionId,
               InvestorPubKey = o.ScriptHex.Substring(0, 32)
            })
         .ToListAsync();

      return true;
   }
}
