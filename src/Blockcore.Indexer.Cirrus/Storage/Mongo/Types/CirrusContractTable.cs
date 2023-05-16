using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types
{
   public class CirrusContractTable
   {
      /// <summary>
      /// The transaction id where the contract was created in.
      /// </summary>
      public string TransactionId { get; set; }

      /// <summary>
      /// State after execution.
      /// </summary>
      public string PostState { get; set; }

      /// <summary>
      /// The block that this was confirmed in, this is to be able to reorg the data.
      /// </summary>
      public long BlockIndex { get; set; }

      /// <summary>
      /// Hash of the block that this was confirmed in
      /// </summary>
      public string BlockHash { get; set; }

      /// <summary>
      /// The type of contract (this is normally taken from the assembly metadata).
      /// </summary>
      public string ContractOpcode { get; set; }

      /// <summary>
      /// The type of contract (this is normally taken from the assembly metadata).
      /// </summary>
      public string ContractCodeType { get; set; }

      /// <summary>
      /// The method that is executed on teh smart contract class.
      /// </summary>
      public string MethodName { get; set; }

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

      /// <summary>
      /// Logs as outputed on the contract execution.
      /// </summary>
      public LogResponse[] Logs { get; set; }
   }
}
