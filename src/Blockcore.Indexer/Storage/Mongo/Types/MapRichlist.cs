using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Storage.Mongo.Types
{
  public class MapRichlist
   {
      [BsonId]
      public string Address { get; set; }
      public long Balance { get; set; }
   }
}
