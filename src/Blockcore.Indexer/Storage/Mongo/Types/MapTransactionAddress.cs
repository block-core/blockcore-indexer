namespace Blockcore.Indexer.Storage.Mongo.Types
{
   using System.Collections.Generic;

   public class MapTransactionAddress
   {
      public string Id { get; set; }

      public int Index { get; set; }

      public List<string> Addresses { get; set; }

      public string TransactionId { get; set; }

      public string ScriptHex { get; set; }

      public long Value { get; set; }

      public string SpendingTransactionId { get; set; }

      public long? SpendingBlockIndex { get; set; }

      public long BlockIndex { get; set; }

      public bool CoinBase { get; set; }

      public bool CoinStake { get; set; }
   }
}
