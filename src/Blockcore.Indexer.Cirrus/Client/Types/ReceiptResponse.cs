using System.Collections.Generic;

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

    public class LogResponse
    {
        public string Address { get; set; }
      public string[] Topics { get; set; }
      public string Data { get; set; }

      public LogData Log { get; set; }
    }

    public class LogData
    {
       public string Event { get; set; }

        public IDictionary<string, object> Data { get; set; }
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
