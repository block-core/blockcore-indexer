using Newtonsoft.Json;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class TransactionOutputInfo
   {
      #region Public Properties

      [JsonProperty("value")]
      public decimal Amount { get; set; }

      [JsonProperty("bestblock")]
      public string BestBlock { get; set; }

      [JsonProperty("coinbase")]
      public bool CoinBase { get; set; }

      [JsonProperty("confirmations")]
      public long Confirmations { get; set; }

      [JsonProperty("scriptPubKey")]
      public ScriptPubKey ScriptPubKey { get; set; }

      [JsonProperty("version")]
      public long Version { get; set; }

      #endregion
   }
}
