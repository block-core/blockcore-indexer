namespace Blockcore.Indexer.Cirrus.Models;

public class QueryStandardToken
{
   public string Name { get; set; }

   public string Symbol { get; set; }

   public long TotalSupply { get; set; }
   public string Address { get; set; }
   public long Amount { get; set; }
}
