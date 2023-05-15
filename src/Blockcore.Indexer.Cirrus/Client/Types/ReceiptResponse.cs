namespace Blockcore.Indexer.Cirrus.Client.Types
{
   public class ReceiptResponse
   {
      public string TransactionHash { get; set; }
      public string BlockHash { get; set; }
      public ulong? BlockNumber { get; set; }
      public string PostState { get; set; }
      public ulong GasUsed { get; set; }
      public string From { get; set; }
      public string To { get; set; }
      public string NewContractAddress { get; set; }
      public bool Success { get; set; }
      public string ReturnValue { get; set; }
      public string Bloom { get; set; }
      public string Error { get; set; }
      public LogResponse[] Logs { get; set; }

   }

   //public class LocalExecutionResponse
   //{
   //    public IReadOnlyList<TransferResponse> InternalTransfers { get; set; }

   //    public Stratis.SmartContracts.RuntimeObserver.Gas GasConsumed { get; set; }

   //    public bool Revert { get; set; }

   //    public ContractErrorMessage ErrorMessage { get; set; }

   //    public object Return { get; set; }

   //    public IReadOnlyList<LogResponse> Logs { get; set; }
   //}

   public class TransferResponse
   {
      public string From { get; set; }

      public string To { get; set; }

      public ulong Value { get; set; }
   }
}
