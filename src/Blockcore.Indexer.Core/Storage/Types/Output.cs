namespace Blockcore.Indexer.Core.Storage.Types;

public class Output
{
   public Outpoint Outpoint { get; set; }

   public string Address { get; set; }

   public string ScriptHex { get; set; }
   public long Value { get; set; }
   public uint BlockIndex { get; set; }
   public bool CoinBase { get; set; }
   public bool CoinStake { get; set; }
}
