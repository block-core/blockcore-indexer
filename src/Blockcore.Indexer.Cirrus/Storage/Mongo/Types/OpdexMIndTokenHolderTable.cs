using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class OpdexMIndTokenHolderTable: SmartContractData
{
   public int PeriodIndex { get; set; }

   public long VaultAmount { get; set; }

   public long MiningAmount { get; set; }

   public long TotalSupply { get; set; }

   public long NextDistributionBlock { get; set; }

   // public string Address { get; set; }
   public List<TokenAmountChange> AmountChangesHistory { get; set; }
}
