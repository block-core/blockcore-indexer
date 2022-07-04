using System.Collections.Generic; 

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class StandardTokenHolderTable : SmartContractData
{
   // public string Address { get; set; }
   public List<StandardTokenAmountChange> AmountChangesHistory { get; set; }
}
