using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class SmartContractData
{
   [BsonId]
   public SmartContractTokenId Id { get; set; }
}
