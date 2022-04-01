namespace Blockcore.Indexer.Cirrus.Models
{
   /// <summary>
   /// Fetch information about a smart contract.
   /// </summary>
   public class QueryContractList
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
      public string ContractCodeType { get; set; }

      /// <summary>
      /// The contract address (if its a call then this is null and instead use the ToAddress)
      /// </summary>
      public string ContractAddress { get; set; }

      /// <summary>
      /// Document any errors when executed. 
      /// </summary>
      public string Error { get; set; }
   }
}
