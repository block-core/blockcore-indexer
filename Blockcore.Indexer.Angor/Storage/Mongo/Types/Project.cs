namespace Blockcore.Indexer.Angor.Storage.Mongo.Types;

public class Project
{
   public string AngorKey { get; set; }
   public string FounderKey { get; set; }
   public int TransactionIndex { get; set; }
   public long BlockIndex { get; set; }
}