using Blockcore.Indexer.Cirrus.Client.Types;

namespace Blockcore.Indexer.Cirrus.Models;

public class QueryBlockSmartContractsLogs
{
   public string TransactionHash { get; set; }
   public string BlockHash { get; set; }
   public long BlockNumber { get; set; }
   public string PostState { get; set; }
   public ulong GasUsed { get; set; }
   public string From { get; set; }
   public string To { get; set; }
   public string NewContractAddress { get; set; }
   public bool Success { get; set; }
   public string ReturnValue { get; set; }
   public string Bloom { get; set; }
   public string Error { get; set; }

   // public string MethodName { get; set; }
   // public string ContractCodeType { get; set; }
   // public string ContractBytecode { get; set; }
   // public string ContractCodeHash { get; set; }
   // public string ContractCSharp { get; set; }
   // public ulong GasPrice { get; set; }
   // public ulong Amount { get; set; }
   // public ulong ContractBalance { get; set; }

   public LogResponse[] Logs { get; set; }
}
