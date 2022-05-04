namespace Blockcore.Indexer.Cirrus.Models
{
   /// <summary>
   /// Fetch information about a smart contract represented by
   /// the contract address or the transaction the contract was created in.
   /// </summary>
   public class QueryContractCall
   {
      /// <summary>
      /// The transaction id where the contract was created in.
      /// </summary>
      public string TransactionId { get; set; }

      /// <summary>
      /// The block that this was confirmed in, this is to be able to reorg the data.
      /// </summary>
      public long BlockIndex { get; set; }

      /// <summary>
      /// Was the contract executed successfully (i.e not run out foo gas).
      /// </summary>
      public bool Success { get; set; }

      /// <summary>
      /// How much gas was used to execute the contract.
      /// </summary>
      public ulong GasUsed { get; set; }

      /// <summary>
      /// The price of gas at execution time.
      /// </summary>
      public ulong GasPrice { get; set; }

      /// <summary>
      /// The amount that was transfered in to the contract.
      /// </summary>
      public ulong Amount { get; set; }

      /// <summary>
      /// The balance of the contract after this trx as executed.
      /// </summary>
      public ulong ContractBalance { get; set; }

      /// <summary>
      /// The method that is executed on teh smart contract class.
      /// </summary>
      public string MethodName { get; set; }

      /// <summary>
      /// The address that created the contract.
      /// </summary>
      public string FromAddress { get; set; }

      /// <summary>
      /// Who is the recipient.
      /// </summary>
      public string ToAddress { get; set; }

      /// <summary>
      /// Document any errors when executed. 
      /// </summary>
      public string Error { get; set; }
   }
}
