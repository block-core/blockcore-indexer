using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class StandardTokenComputedTable : SmartContractComputedBase
{
   public override string ContractType { get; } = "StandardToken";

   public string Name { get; set; }

   public string Symbol { get; set; }

   public long TotalSupply { get; set; }

   public long Decimals { get; set; }


   public List<StandardTokenHolder> TokenHolders { get; set; } = new();
}
