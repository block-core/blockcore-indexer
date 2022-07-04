using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Models;

public class QueryNonFungibleTokenContract
{
   public string Name { get; set; }
   public string Symbol { get; set; }
   public string Owner { get; set; }
   public bool OwnerOnlyMinting { get; set; }
   public string PendingOwner { get; set; }
   public List<string> PreviousOwners { get; set; }
   public List<NonFungibleTokenTable> Tokens { get; set; }
}
