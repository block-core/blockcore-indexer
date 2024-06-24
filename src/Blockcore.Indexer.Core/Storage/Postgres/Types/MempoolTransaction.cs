using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class MempoolInput
   {
      public Outpoint outpoint { get; set; }
      public string Address { get; set; }
      public long Value { get; set; }
      public string Txid { get; set; }
      public uint Vout { get; set; }
      public virtual MempoolTransaction Transaction { get; set; }
   }
   public class MempoolOutput
   {
      public Outpoint outpoint { get; set; }
      public string Address { get; set; }
      public string ScriptHex { get; set; }
      public long Value { get; set; }
      public long BlockIndex { get; set; }
      public bool CoinBase { get; set; }
      public bool CoinStake { get; set; }
      public virtual MempoolTransaction Transaction { get; set; }
   }
   public class MempoolTransaction
   {
      public long FirstSeen { get; set; }
      public string Txid { get; set; }
      public ICollection<MempoolInput> Inputs { get; set; }
      public ICollection<MempoolOutput> Outputs { get; set; }
      public List<string> AddressOutputs { get; set; }
      public List<string> AddressInputs { get; set;}

   }
}
