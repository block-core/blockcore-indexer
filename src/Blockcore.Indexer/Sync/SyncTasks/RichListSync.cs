using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Sync.SyncTasks
{
   public class RichListSync : TaskRunner
   {
      private readonly MongoData mongoData;
      private readonly ILogger<RichListSync> log;

      private readonly Stopwatch watch;

      private bool syncInProgress;
      DateTime lastSync;

      public RichListSync(IOptions<IndexerSettings> configuration, ILogger<RichListSync> logger,IStorage data)
         : base(configuration, logger)
      {
         mongoData = (MongoData)data;
         log = logger;
         watch = new Stopwatch();
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

            var pipeline = BuildRichListComputingAndTableUpdatePipeline();

            await mongoData.MapBlock.AggregateAsync(pipeline);

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
         return !(//sync with other runners
            !Runner.SyncingBlocks.IndexModeCompleted ||
                  Runner.SyncingBlocks.Blocked ||
                  Runner.SyncingBlocks.ReorgMode ||
                  Runner.SyncingBlocks.StoreTip == null ||
                  Runner.SyncingBlocks.IndexMode||
            //local state valid
            syncInProgress ||
                  lastSync.AddHours(1) > DateTime.UtcNow);
      }

      private PipelineDefinition<MapBlock,object> BuildRichListComputingAndTableUpdatePipeline()
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
                  { "from", "AddressForOutput" },
                  {
                     "pipeline", new BsonArray(new List<BsonDocument>
                     {
                        new BsonDocument("$group", new BsonDocument("_id", "$Address")),
                        new BsonDocument("$lookup",
                           new BsonDocument
                           {
                              { "from", "AddressForOutput" },
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
                  { "from", "AddressForInput" },
                  {
                     "pipeline", new BsonArray(new List<BsonDocument>
                     {
                        new BsonDocument("$group", new BsonDocument("_id", "$Address")),
                        new BsonDocument("$lookup", new BsonDocument
                        {
                           { "from", "AddressForInput" },
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
               new BsonDocument { { "_id", "$_id" }, { "Value", new BsonDocument("$sum", "$Value") } }),
            new BsonDocument("$match", new BsonDocument("Value",new BsonDocument("$gt",0))),
            new BsonDocument("$sort", new BsonDocument("Value", -1)),
            new BsonDocument("$limit", 250),

            //output to rich list and replace existing
            new BsonDocument("$out", "RichList")
         };
      }
   }
}
