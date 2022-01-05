using System.Collections.Generic;
using System.Linq;

namespace Blockcore.Indexer.Core.Client.Types
{
   #region Using Directives

   #endregion

   public class CreateRawTransaction
   {
      #region Constructors and Destructors

      public CreateRawTransaction()
      {
         Inputs = new List<CreateRawTransactionInput>();
         Outputs = new Dictionary<string, decimal>();
      }

      #endregion

      #region Public Properties

      public List<CreateRawTransactionInput> Inputs { get; set; }

      public Dictionary<string, decimal> Outputs { get; set; }

      #endregion

      #region Public Methods and Operators

      public void AddInput(string transactionId, int output)
      {
         Inputs.Add(new CreateRawTransactionInput { TransactionId = transactionId, Output = output });
      }

      public void AddOutput(string address, decimal amount)
      {
         Outputs.Add(address, amount);
      }

      public void ReduceFeeFromAddress(string address, decimal fee)
      {
         KeyValuePair<string, decimal> output = Outputs.First(f => f.Key == address);
         decimal amont = output.Value;
         Outputs[output.Key] = amont - fee;
      }

      #endregion
   }
}
