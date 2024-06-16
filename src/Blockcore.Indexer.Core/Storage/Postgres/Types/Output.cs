namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class Output
   {
      // public string Txid { get; set; } //foreign key
      // public uint Vout { get; set; }
      public Outpoint outpoint { get; set; }
      public string Address { get; set; }
      public string ScriptHex { get; set; }
      public long Value { get; set; }
      public uint BlockIndex { get; set; }
      public bool CoinBase { get; set; }
      public bool CoinStake { get; set; }
      public virtual Transaction Transaction { get; set; }
   }
}
