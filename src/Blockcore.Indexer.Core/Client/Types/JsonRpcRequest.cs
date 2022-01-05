using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class JsonRpcRequest
   {
      #region Constructors and Destructors

      public JsonRpcRequest(int id, string method, params object[] parameters)
      {
         Id = id;
         Method = method;

         if (parameters != null)
         {
            Parameters = parameters.ToList();
         }
         else
         {
            Parameters = new List<object>();
         }
      }

      #endregion

      #region Public Properties

      [JsonProperty(PropertyName = "id", Order = 2)]
      public int Id { get; set; }

      [JsonProperty(PropertyName = "method", Order = 0)]
      public string Method { get; set; }

      [JsonProperty(PropertyName = "params", Order = 1)]
      public IList<object> Parameters { get; set; }

      #endregion

      #region Public Methods and Operators

      public byte[] GetBytes()
      {
         string json = JsonConvert.SerializeObject(this);
         return Encoding.UTF8.GetBytes(json);
      }

      #endregion
   }
}
