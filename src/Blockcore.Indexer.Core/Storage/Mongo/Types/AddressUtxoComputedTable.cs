namespace Blockcore.Indexer.Core.Storage.Mongo.Types
{
   /// <summary>
   /// This table is not used anymore to store utxo data in mongodb,
   /// however its used in the computation table calculate the utxo count. 
   /// </summary>
   public class AddressUtxoComputedTable
   {
      public Outpoint Outpoint { get; set; }

      public string Address { get; set; }

      public string ScriptHex { get; set; }
      public long Value { get; set; }
      public long BlockIndex { get; set; }
      public bool CoinBase { get; set; }
      public bool CoinStake { get; set; }
   }
}
