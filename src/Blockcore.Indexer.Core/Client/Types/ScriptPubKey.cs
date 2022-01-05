using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class ScriptPubKey
   {
      #region Public Properties

      public List<string> Addresses { get; set; }

      public string Asm { get; set; }

      public string Hex { get; set; }

      public int ReqSigs { get; set; }

      public string Type { get; set; }

      #endregion
   }
}
