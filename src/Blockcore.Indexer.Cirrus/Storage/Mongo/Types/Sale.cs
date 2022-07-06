using MongoDB.Bson.Serialization.Attributes;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(OwnershipTransfer),typeof(OnSale),typeof(Auction))]
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

   public bool Sold { get; set; }
   public string Buyer { get; set; }
   public string PurchaseTransactionId { get; set; }
}

public class Auction : TokenSaleEvent
{
   public string Seller { get; set; }
   public long StartingPrice { get; set; }
   public long EndBlock { get; set; }
   public bool AuctionEnded { get; set; }

   public string HighestBidder { get; set; }
   public long HighestBid { get; set; }

   public string HighestBidTransactionId { get; set; }
   public bool Success { get; set; }
}
