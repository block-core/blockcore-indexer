namespace Blockcore.Indexer.Core.Models;

public class QueryAddressBalance
{
   public string Address { get; set; }

   public long Balance { get; set; }

   public long PendingSent { get; set; }

   public long PendingReceived { get; set; }
}
