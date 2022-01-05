using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Core.Storage.Mongo.Types
{
  public class RichlistTable
   {
      [BsonId]
      public string Address { get; set; }
      public long Balance { get; set; }
   }
}
