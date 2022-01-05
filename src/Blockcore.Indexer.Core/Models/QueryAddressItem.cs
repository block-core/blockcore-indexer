namespace Blockcore.Indexer.Core.Models
{
   public class QueryAddressItem
   {
      /// <summary>
      /// Gets or sets the type.
      /// </summary>
      public string EntryType { get; set; }

      /// <summary>
      /// Gets or sets the transaction hash.
      /// </summary>
      public string TransactionHash { get; set; }

      /// <summary>
      /// Gets or sets the amount.
      /// </summary>
      public long Value { get; set; }

      /// <summary>
      /// Gets or sets the block index if included in a block.
      /// </summary>
      public long? BlockIndex { get; set; }

      /// <summary>
      /// Gets or sets the confirmations.
      /// </summary>
      public long? Confirmations { get; set; }
   }
}
