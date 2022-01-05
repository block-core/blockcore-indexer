using Newtonsoft.Json;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class JsonRpcResponse<T>
   {
      #region Constructors and Destructors

      public JsonRpcResponse(int id, JsonRpcError error, T result)
      {
         Id = id;
         Error = error;
         Result = result;
      }

      #endregion

      #region Public Properties

      [JsonProperty(PropertyName = "error", Order = 2)]
      public JsonRpcError Error { get; set; }

      [JsonProperty(PropertyName = "id", Order = 1)]
      public int Id { get; set; }

      [JsonProperty(PropertyName = "result", Order = 0)]
      public T Result { get; set; }

      #endregion
   }
}
