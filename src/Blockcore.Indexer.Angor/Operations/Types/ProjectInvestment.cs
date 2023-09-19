namespace Blockcore.Indexer.Angor.Operations.Types;

public class ProjectInvestment
{
   public string InvestorPublicKey { get; set; }
   public long TotalAmount { get; set; }
   public string TransactionId { get; set; }

   public string HashOfSecret { get; set; }

   public bool IsSeeder => !string.IsNullOrEmpty(HashOfSecret);
}
