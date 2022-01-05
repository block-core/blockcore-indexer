using Newtonsoft.Json;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class JsonRpcError
   {
      #region Public Properties

      [JsonProperty(PropertyName = "code", Order = 0)]
      public int Code { get; set; }

      [JsonProperty(PropertyName = "message", Order = 1)]
      public string Message { get; set; }

      #endregion
   }
}
