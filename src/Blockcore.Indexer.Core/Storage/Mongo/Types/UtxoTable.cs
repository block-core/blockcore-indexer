using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Core.Storage.Mongo.Types;

[BsonIgnoreExtraElements]
public class UtxoTable
{
   public Outpoint Outpoint { get; set; }

   public string Address { get; set; }

   public long Value { get; set; }

   public long BLockIndex { get; set; }
}
