namespace Blockcore.Indexer.Angor.Operations.Types;

public class ProjectIndexerData
{
   public string FounderKey { get; set; }
   public string NostrPubKey { get; set; }
   public string ProjectIdentifier { get; set; }
   public string TrxHex { get; set; }

   public string TrxId { get; set; }
   public long? TotalInvestmentsCount { get; set; }
}
