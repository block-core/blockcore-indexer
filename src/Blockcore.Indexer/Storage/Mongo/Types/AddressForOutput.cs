using System.Collections.Generic;

namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class AddressForOutput
   {
      public string Outpoint { get; set; }
      //public List<string> Addresses { get; set; }
      public string Address { get; set; }

      public string ScriptHex { get; set; }
      public long Value { get; set; }
      public long BlockIndex { get; set; }
      public bool CoinBase { get; set; }
      public bool CoinStake { get; set; }
   }
}
