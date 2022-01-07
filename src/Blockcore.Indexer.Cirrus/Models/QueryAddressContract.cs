namespace Blockcore.Indexer.Cirrus.Models
{
   /// <summary>
   /// Fetch information about a smart contract represented by
   /// the contract address or the transaction the contract was created in.
   /// </summary>
   public class QueryAddressContract
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
      /// The type of contract (this is normally taken from the assembly metadata).
      /// </summary>
      public string ContractType { get; set; }

      /// <summary>
      /// Was the contract executed successfully (i.e not run out foo gas).
      /// </summary>
      public bool Success { get; set; }

      /// <summary>
      /// How much gas was used to execute the contract.
      /// </summary>
      public ulong GasUsed { get; set; }

      /// <summary>
      /// The address that created the contract.
      /// </summary>
      public string FromAddress { get; set; }

      /// <summary>
      /// Who is the recipient.
      /// </summary>
      public string ToAddress { get; set; }

      /// <summary>
      /// The contract address (if its a call then this is null and instead use the ToAddress)
      /// </summary>
      public string NewContractAddress { get; set; }

      /// <summary>
      /// Document any errors when executed. 
      /// </summary>
      public string Error { get; set; }
   }
}
