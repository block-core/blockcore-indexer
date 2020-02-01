namespace Blockcore.Indexer.Settings
{
   public class IndexerSettings
   {
      public string RpcPassword { get; set; }

      public int SyncInterval { get; set; }

      public int MaxItemsInQueue { get; set; }

      public int ParallelRequestsToTransactionRpc { get; set; }

      public int RpcAccessPort { get; set; }

      public bool RpcSecure { get; set; }

      public string RpcDomain { get; set; }

      public bool SyncBlockchain { get; set; }

      public long StartBlockIndex { get; set; }

      public bool SyncMemoryPool { get; set; }

      public string RpcUser { get; set; }

      public string NotifyUrl { get; set; }

      public string ConnectionString { get; set; }

      public bool DatabaseNameSubfix { get; set; }

      public int NotifyBatchCount { get; set; }

      public int MongoBatchSize { get; set; }

      public int AverageInterval { get; set; }

      public bool StoreRawTransactions { get; set; }
   }
}
