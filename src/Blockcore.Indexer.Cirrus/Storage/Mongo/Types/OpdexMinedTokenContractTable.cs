namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class OpdexMinedTokenContractTable : SmartContractTable
{
   public long CreatedOnBlock { get; set; }
   public string CreatorAddress { get; set; }
}
