using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using NBitcoin;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types
{
   public class CirrusContractCodeTable
   {
      public string ContractAddress { get; set; }

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
      public string SourceCode { get; set; }

      /// <summary>
      /// The hash of the contract.
      /// </summary>
      public string ContractHash { get; set; }

      /// <summary>
      /// The block the contract was whitelisted.
      /// </summary>
      public long BlockIndex { get; set; }
   }
}
