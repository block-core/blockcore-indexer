namespace Blockcore.Indexer.Api.Handlers.Types
{
   using System.Collections.Generic;

   public class QueryAddressResults
   {
      public int Total { get; set; }

      /// <summary>
      /// Gets or sets the transactions.
      /// </summary>
      public IEnumerable<QueryAddressItem> Transactions { get; set; }

      /// <summary>
      /// Gets or sets the Unconfirmed transactions.
      /// </summary>
      public IEnumerable<QueryAddressItem> UnconfirmedTransactions { get; set; }
   }

   public class QueryAddress
   {
      /// <summary>
      /// Gets or sets the Symbol.
      /// </summary>
      public string Symbol { get; set; }

      /// <summary>
      /// Gets or sets the address.
      /// </summary>
      public string Address { get; set; }

      /// <summary>
      /// Gets or sets the balance.
      /// </summary>
      public long Balance { get; set; }

      /// <summary>
      /// Gets or sets the total received.
      /// </summary>
      public long? TotalReceived { get; set; }

      /// <summary>
      /// Gets or sets the total sent.
      /// </summary>
      public long? TotalSent { get; set; }

      /// <summary>
      /// Gets or sets the unconfirmed balance.
      /// </summary>
      public long UnconfirmedBalance { get; set; }
   }
}
