using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Client.Types
{
   public class ContractReceiptResponse : ReceiptResponse
   {
      public string MethodName { get; set; }
      public string ContractCodeType { get; set; }
   }

}
