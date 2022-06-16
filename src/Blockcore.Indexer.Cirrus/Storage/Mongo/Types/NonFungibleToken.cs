using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class NonFungibleToken
{
   public SmartContractTokenId Id { get; set; }
   public string Creator { get; set; }
   public string Owner { get; set; }
   public string Uri { get; set; }
   public bool IsBurned { get; set; }

   public List<TokenSaleEvent> SalesHistory { get; set; } = new();
}
