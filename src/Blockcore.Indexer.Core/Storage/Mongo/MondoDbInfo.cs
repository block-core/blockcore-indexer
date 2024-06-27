using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public class MondoDbInfo : IMondoDbInfo
{
   private readonly IMongoDatabase mongoDatabase;

   public MondoDbInfo(IMongoDatabase mongoDatabase)
   {
      this.mongoDatabase = mongoDatabase;
   }

   public List<IndexView> GetIndexesBuildProgress()
   {
      IMongoDatabase db = mongoDatabase.Client.GetDatabase("admin");
      var command = new BsonDocument {
         { "currentOp", "1"},
      };
      BsonDocument currentOp = db.RunCommand<BsonDocument>(command);

      BsonElement inproc = currentOp.GetElement(0);
      var arr = inproc.Value as BsonArray;

      var ret = new List<IndexView>();

      foreach (BsonValue bsonValue in arr)
      {
         BsonElement? desc = bsonValue.AsBsonDocument?.GetElement("desc");
         if (desc != null)
         {
            bool track = desc?.Value.AsString.Contains("IndexBuildsCoordinatorMongod") ?? false;

            if (track)
            {
               var indexed = new IndexView {Msg = bsonValue.AsBsonDocument?.GetElement("msg").Value.ToString()};

               BsonElement? commandElement = bsonValue.AsBsonDocument?.GetElement("command");

               string dbName = string.Empty;
               if (commandElement.HasValue)
               {
                  BsonDocument bsn = commandElement.Value.Value.AsBsonDocument;
                  dbName = bsn.GetElement("$db").Value.ToString();
                  indexed.Command = $"{bsn.GetElement(0).Value}-{bsn.GetElement(1).Value}";
               }

               if (dbName == mongoDatabase.DatabaseNamespace.DatabaseName)
               {
                  ret.Add(indexed);
               }

            }
         }
      }

      return ret;
   }
}
