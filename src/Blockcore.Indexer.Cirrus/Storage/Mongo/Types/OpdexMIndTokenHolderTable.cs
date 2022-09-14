using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class OpdexMIndTokenHolderTable: SmartContractData
{
   // public string Address { get; set; }
   public List<TokenAmountChange> AmountChangesHistory { get; set; }
}
