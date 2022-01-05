namespace Blockcore.Indexer.Core.Storage.Types
{
   public class SyncTransactionAddressItem
   {
      public int Index { get; set; }

      public string Address { get; set; }

      public string Type { get; set; }

      public string TransactionHash { get; set; }

      public string SpendingTransactionHash { get; set; }

      public long? SpendingBlockIndex { get; set; }

      public string ScriptHex { get; set; }

      public bool CoinBase { get; set; }

      public bool CoinStake { get; set; }

      public long? BlockIndex { get; set; }

      public long? Confirmations { get; set; }

      public long Value { get; set; }

      public long Time { get; set; }

   }
}
