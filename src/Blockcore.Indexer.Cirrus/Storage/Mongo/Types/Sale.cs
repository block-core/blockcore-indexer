namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class TokenSaleEvent
{
   public string TransactionId { get; set; }
}

public class OwnershipTransfer : TokenSaleEvent
{
   public string From { get; set; }
   public string To { get; set; }
}

public class OnSale : TokenSaleEvent
{
   public string Seller { get; set; }
   public long Price { get; set; }

}

public class Auction : TokenSaleEvent
{
   public string Seller { get; set; }
   public long StartingPrice { get; set; }
   public long EndBlock { get; set; }
}
