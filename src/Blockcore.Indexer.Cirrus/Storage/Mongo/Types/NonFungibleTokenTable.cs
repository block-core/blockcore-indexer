using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class NonFungibleTokenTable : SmartContractData
{
   public string Creator { get; set; }
   public string Owner { get; set; }
   public string Uri { get; set; }
   public bool IsBurned { get; set; }
   public bool IsUsed { get; set; }
   public List<TokenSaleEvent> SalesHistory { get; set; } = new();
}
