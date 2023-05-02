using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Models;

public class QueryAddressAsset
{
   public string Id { get; set; }
   public string Creator { get; set; }
   public string Uri { get; set; }
   public bool IsBurned { get; set; }

   public bool IsUsed { get; set; }
   public long? PricePaid { get; set; }
   public string TransactionId { get; set; }

   public TokenSaleEvent TokenSaleEvent { get; set; }
   public string ContractId { get; set; }
}
