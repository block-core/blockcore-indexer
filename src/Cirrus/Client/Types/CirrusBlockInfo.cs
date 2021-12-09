using Blockcore.Indexer.Client.Types;
using Newtonsoft.Json;

namespace Cirrus.Client.Types
{
   public class CirrusBlockInfo : BlockInfo
   {
      [JsonProperty("HashStateRoot")]
      public string HashStateRoot { get; set; }
      [JsonProperty("ReceiptRoot")]
      public string ReceiptRoot{ get; set; }
      [JsonProperty("Bloom")]
      public string Bloom { get; set; }
   }
}
