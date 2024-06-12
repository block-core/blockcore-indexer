using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Angor.Storage.Mongo.Types;

public class Investment
{
   public InvestorType Type => string.IsNullOrEmpty(SecretHash) ? InvestorType.Investor : InvestorType.Seeder;

   public string AngorKey { get; set; }
   public string InvestorPubKey { get; set; }
   public string SecretHash { get; set; }

   public long BlockIndex { get; set; }

   public string TransactionId { get; set; }

   public long AmountSats { get; set; }

   public List<Outpoint> StageOutpoint { get; set; }
}
