namespace Blockcore.Indexer.Api.Handlers.Types
{
   using Blockcore.Indexer.Client.Types;

   public class CoinInfo
   {
      public long BlockHeight { get; set; }

      public string Name { get; set; }

      public string Symbol { get; set; }

      public string Description { get; set; }

      public string Url { get; set; }

      public string Logo { get; set; }

      public string Icon { get; set; }
   }

   public class Statistics
   {
      /// <summary>
      /// Gets or sets the coin tag.
      /// </summary>
      public string Symbol { get; set; }

      /// <summary>
      /// Gets or sets the sync progress.
      /// </summary>
      public string Progress { get; set; }

      /// <summary>
      /// Gets or sets the number of transactions in pool.
      /// </summary>
      public int TransactionsInPool { get; set; }

      /// <summary>
      /// Gets or sets the current block index.
      /// </summary>
      public long SyncBlockIndex { get; set; }

      public BlockchainInfoModel BlockchainInfo { get; set; }

      public NetworkInfoModel NetworkInfo { get; set; }

      public int BlocksPerMinute { get; set; }

      public double AvgBlockPersistInSeconds { get; set; }

      public double AvgBlockSizeKb { get; set; }

      public string Error { get; set; }

   }
}
