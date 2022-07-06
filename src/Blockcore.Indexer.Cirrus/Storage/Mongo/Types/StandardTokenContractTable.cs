namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class StandardTokenContractTable : SmartContractTable
{
   public string Name { get; set; }

   public string Symbol { get; set; }

   public long TotalSupply { get; set; }

   public long Decimals { get; set; }
   public long CreatedOnBlock { get; set; }
   public string CreatorAddress { get; set; }
}
