using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class StandardTokenHolderTable
{
   [BsonId]
   public SmartContractTokenId Id { get; set; }
   // public string Address { get; set; }
   public List<StandardTokenAmountChange> AmountChangesHistory { get; set; }
}
