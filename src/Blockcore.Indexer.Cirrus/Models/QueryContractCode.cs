namespace Blockcore.Indexer.Cirrus.Models
{
   public class QueryContractCode
   {
      /// <summary>
      /// The smart contract ype.
      /// </summary>
      public string CodeType { get; set; }

      /// <summary>
      /// The smart contract byte code.
      /// </summary>
      public string ByteCode { get; set; }

      /// <summary>
      /// The smart contract csharp code.
      /// </summary>
      public string Csharp { get; set; }

      /// <summary>
      /// The hash of the contract.
      /// </summary>
      public string ContractHash { get; set; }

   }
}
