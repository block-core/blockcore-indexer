namespace Blockcore.Indexer.Core.Models
{
   public class QueryAddress
   {
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
      public long TotalReceived { get; set; }

      /// <summary>
      /// Gets or sets the total received.
      /// </summary>
      public long TotalStake { get; set; }

      /// <summary>
      /// Gets or sets the total received.
      /// </summary>
      public long TotalMine { get; set; }

      /// <summary>
      /// Gets or sets the total sent.
      /// </summary>
      public long TotalSent { get; set; }

      /// <summary>
      /// Gets or sets the unconfirmed balance.
      /// </summary>
      public long TotalReceivedCount { get; set; }

      /// <summary>
      /// Gets or sets the unconfirmed balance.
      /// </summary>
      public long TotalSentCount { get; set; }

      public long TotalStakeCount { get; set; }

      public long TotalMineCount { get; set; }

      public long PendingSent { get; set; }

      public long PendingReceived { get; set; }
   }
}
