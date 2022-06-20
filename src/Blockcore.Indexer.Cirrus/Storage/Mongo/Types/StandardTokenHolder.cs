using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class StandardTokenHolder
{
   [BsonId]
   public SmartContractTokenId Id { get; set; }
   // public string Address { get; set; }
   public List<StandardTokenAmountChange> AmountChangesHistory { get; set; }
}
