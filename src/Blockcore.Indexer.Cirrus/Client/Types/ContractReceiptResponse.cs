using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Client.Types
{
   public class ContractReceiptResponse : ReceiptResponse
   {
      public string MethodName { get; set; }
      public string ContractCodeType { get; set; }
      public string ContractBytecode { get; set; }
      public string ContractCodeHash { get; set; }
      public string ContractCSharp { get; set; }
      public ulong GasPrice { get; set; }
      public ulong Amount { get; set; }
      public ulong ContractBalance { get; set; }

      public Dictionary<string, object> ContractData { get; set; }
   }
}
