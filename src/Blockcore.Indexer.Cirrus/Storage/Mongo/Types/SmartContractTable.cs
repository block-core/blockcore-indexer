using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public abstract class SmartContractTable
{
   public string ContractAddress { get; set; }
   public string ContractCreateTransactionId { get; set; }
   public long LastProcessedBlockHeight { get; set; }
}
