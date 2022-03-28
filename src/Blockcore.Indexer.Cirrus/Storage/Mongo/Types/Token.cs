namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class Token
{
   public long Id { get; set; }
   public string Address { get; set; }
   public string Uri { get; set; }

   public string OwnerAddress { get; set; }
   public bool IsBurned { get; set; }
}
