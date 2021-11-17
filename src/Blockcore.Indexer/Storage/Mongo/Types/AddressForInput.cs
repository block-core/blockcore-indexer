using System.Collections.Generic;

namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class AddressForInput
   {
      public Outpoint Outpoint { get; set; }

      public string Address { get; set; }

      public long Value { get; set; }

      public string TrxHash { get; set; }

      public long BlockIndex { get; set; }
   }
}
