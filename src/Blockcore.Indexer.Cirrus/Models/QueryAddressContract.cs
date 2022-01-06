namespace Blockcore.Indexer.Cirrus.Models
{
   public class QueryAddressContract
   {
      public string TransactionId { get; set; }
      public long BlockIndex { get; set; }
      public string ContractType { get; set; }
      public string MethodName { get; set; }
      public bool Success { get; set; }
      public ulong GasUsed { get; set; }
      public string FromAddress { get; set; }
      public string ToAddress { get; set; }
      public string NewContractAddress { get; set; }
      public string Error { get; set; }
   }
}
