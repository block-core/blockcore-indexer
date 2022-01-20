namespace Blockcore.Indexer.Core.Storage.Mongo.Types;

public class UnspentOutputTable
{
   public Outpoint Outpoint { get; set; }

   public string Address { get; set; }

   public long Value { get; set; }

   public long BlockIndex { get; set; }
}
