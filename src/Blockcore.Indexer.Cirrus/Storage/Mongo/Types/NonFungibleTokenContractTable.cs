using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class NonFungibleTokenContractTable : SmartContractTable
{
   public string Name { get; set; }
   public string Symbol { get; set; }
   public string Owner { get; set; }
   public bool OwnerOnlyMinting { get; set; }
   public string PendingOwner { get; set; }
   public List<string> PreviousOwners { get; set; }
}
