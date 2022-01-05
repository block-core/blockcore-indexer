using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class DecodedRawTransaction
   {
      #region Public Properties

      public string Hex { get; set; }

      public long Locktime { get; set; }

      public string TxId { get; set; }

      public List<Vin> VIn { get; set; }

      public List<Vout> VOut { get; set; }

      public int Version { get; set; }

      #endregion
   }
}
