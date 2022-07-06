namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class StandardTokenAmountChange
{
   public long BlockIndex { get; set; } //This is for rewinds
   public string TransactionId { get; set; }
   public long Amount { get; set; }
}
