using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   public class RichListSync : TaskRunner
   {
      private readonly IMongoDb db;
      private readonly ILogger<RichListSync> log;
      private readonly IndexerSettings indexerSettings;

      private readonly Stopwatch watch;

      private bool syncInProgress;
      DateTime lastSync;

      public RichListSync(IOptions<IndexerSettings> configuration, ILogger<RichListSync> logger,IMongoDb data)
         : base(configuration, logger)
      {
         db = data;
         log = logger;
         watch = new Stopwatch();
         indexerSettings = configuration.Value;
      }

      public override async Task<bool> OnExecute()
      {
         if (!CanRunRichListSync())
         {
            return false;
         }

         try
         {
            syncInProgress = true;

            log.LogDebug($"Starting rich list computations");

            watch.Restart();

            await db.UnspentOutputTable.Aggregate(new AggregateOptions { AllowDiskUse = true })
               .Group(table => table.Address,
                  tables => new { Address = tables.Key, Balance = tables.Sum(table => table.Value) })
               .SortByDescending(arg => arg.Balance)
               .Limit(1000)
               .OutAsync(db.RichlistTable.CollectionNamespace.CollectionName);

            watch.Stop();
            log.LogDebug($"Finished updating rich list in {watch.Elapsed}");
         }
         catch (Exception e)
         {
            log.LogError(e,"Failed to sync the rich list table");
            lastSync = new DateTime();
         }
         finally
         {
            syncInProgress = false;
         }

         lastSync = DateTime.UtcNow;
         return await Task.FromResult(false);
      }

      private bool CanRunRichListSync()
      {
         if (indexerSettings.SyncRichlist == false)
         {
            Abort = true;
            return false;
         }

         return !(//sync with other runners
            !Runner.GlobalState.IndexModeCompleted ||
                  Runner.GlobalState.Blocked ||
                  Runner.GlobalState.ReorgMode ||
                  Runner.GlobalState.StoreTip == null ||
                  Runner.GlobalState.IndexMode||
            //local state valid
            syncInProgress ||
                  lastSync.AddMinutes(10) > DateTime.UtcNow);
      }

      private PipelineDefinition<BlockTable,object> BuildRichListComputingAndTableUpdatePipeline()
      {
         return new[]
         {
            // new empty document
            new BsonDocument("$limit", 1),
            new BsonDocument("$project", new BsonDocument { { "_id", "$$REMOVE" } }),

            //Output table extraction
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Output" },
                  {
                     "pipeline", new BsonArray(new List<BsonDocument>
                     {
                        new BsonDocument("$group", new BsonDocument("_id", "$Address")),
                        new BsonDocument("$lookup",
                           new BsonDocument
                           {
                              { "from", "Output" },
                              { "localField", "_id" },
                              { "foreignField", "Address" },
                              {
                                 "pipeline", new BsonArray(new List<BsonDocument>
                                 {
                                    new BsonDocument("$group",
                                       new BsonDocument
                                       {
                                          { "_id", "$Address" },
                                          { "Value", new BsonDocument("$sum", "$Value") }
                                       })
                                 })
                              },
                              { "as", "Data" }
                           }),
                        new BsonDocument("$unwind",
                           new BsonDocument { { "path", "$Data" }, { "preserveNullAndEmptyArrays", true } }),
                        new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$Data")),
                     })
                  },
                  { "as", "Outputs" }
               }),

            //Input table extraction
            new BsonDocument("$lookup",
               new BsonDocument
               {
                  { "from", "Input" },
                  {
                     "pipeline", new BsonArray(new List<BsonDocument>
                     {
                        new BsonDocument("$group", new BsonDocument("_id", "$Address")),
                        new BsonDocument("$lookup", new BsonDocument
                        {
                           { "from", "Input" },
                           { "localField", "_id" },
                           { "foreignField", "Address" },
                           {
                              "pipeline", new BsonArray(new List<BsonDocument>
                              {
                                 new BsonDocument("$group", new BsonDocument
                                 {
                                    { "_id", "$Address" },
                                    {
                                       "Value", new BsonDocument("$sum",
                                          new BsonDocument("$subtract",
                                             new BsonArray(new List<BsonValue> { 0, "$Value" })))
                                    }
                                 })
                              })
                           },
                           { "as", "Data" }
                        }),
                        new BsonDocument("$unwind",
                           new BsonDocument { { "path", "$Data" }, { "preserveNullAndEmptyArrays", true } }),
                        new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$Data"))
                     })
                  },
                  { "as", "Inputs" }
               }),


            //merge of the data
            new BsonDocument("$project", new BsonDocument("union", new BsonDocument(
               "$concatArrays", new BsonArray(new List<string> { "$Outputs", "$Inputs" })))),
            new BsonDocument("$unwind", "$union"),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$union")),

            //sum the values for all addresses order by value desc
            new BsonDocument("$group",
               new BsonDocument { { "_id", "$_id" }, { "Balance", new BsonDocument("$sum", "$Value") } }),
            new BsonDocument("$match", new BsonDocument("Balance",new BsonDocument("$gt",0))),
            new BsonDocument("$sort", new BsonDocument("Balance", -1)),
            new BsonDocument("$limit", 250),

            //output to rich list and replace existing
            new BsonDocument("$out", db.RichlistTable.CollectionNamespace.CollectionName)
         };
      }
   }
}
