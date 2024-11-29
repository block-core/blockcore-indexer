namespace Blockcore.Indexer.Angor.Storage.Mongo.Types;

public class Project
{
   public string AngorKey { get; set; }
   public string FounderKey { get; set; }

   public string NosrtEventId { get; set; }
   public string AngorKeyScriptHex { get; set; }
   public long BlockIndex { get; set; }
   public string TransactionId { get; set; } //TODO check if this should be a lookup

   public string AddressOnFeeOutput { get; set; } // for indexed lookups against the output table
}
