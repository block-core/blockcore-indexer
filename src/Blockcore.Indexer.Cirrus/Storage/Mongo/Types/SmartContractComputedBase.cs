using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public abstract class SmartContractComputedBase
{
   [BsonId]
   public string ContractAddress { get; set; }
   public string ContractCreateTransactionId { get; set; }
   public long LastProcessedBlockHeight { get; set; }

   public abstract string ContractType { get; }
}
