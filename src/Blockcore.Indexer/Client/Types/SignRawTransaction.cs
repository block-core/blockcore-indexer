namespace Blockcore.Indexer.Client.Types
{
   #region Using Directives

   using System.Collections.Generic;

   #endregion

   public class SignRawTransaction
   {
      #region Constructors and Destructors

      public SignRawTransaction(string rawTransactionHex)
      {
         RawTransactionHex = rawTransactionHex;
         Inputs = new List<SignRawTransactionInput>();
         PrivateKeys = new List<string>();
      }

      #endregion

      #region Public Properties

      public List<SignRawTransactionInput> Inputs { get; set; }

      public List<string> PrivateKeys { get; set; }

      public string RawTransactionHex { get; set; }

      #endregion

      #region Public Methods and Operators

      public void AddInput(string transactionId, int output, string scriptPubKey)
      {
         Inputs.Add(new SignRawTransactionInput { TransactionId = transactionId, Output = output, ScriptPubKey = scriptPubKey });
      }

      public void AddKey(string privateKey)
      {
         PrivateKeys.Add(privateKey);
      }

      #endregion
   }
}
