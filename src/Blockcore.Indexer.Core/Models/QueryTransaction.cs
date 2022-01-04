namespace Blockcore.Indexer.Api.Handlers.Types
{
   using System;
   using System.Collections.Generic;

   public class QueryTransaction
   {
      /// <summary>
      /// Gets or sets the Symbol.
      /// </summary>
      public string Symbol { get; set; }

      /// <summary>
      /// Gets or sets the block hash.
      /// </summary>
      public string BlockHash { get; set; }

      /// <summary>
      /// Gets or sets the block index.
      /// </summary>
      public long? BlockIndex { get; set; }

      /// <summary>
      /// Gets or sets the Timestamp.
      /// </summary>
      public long Timestamp { get; set; }

      /// <summary>
      /// Gets or sets the transaction id.
      /// </summary>
      public string TransactionId { get; set; }

      /// <summary>
      /// Gets or sets the confirmations.
      /// </summary>
      public long Confirmations { get; set; }

      public bool IsCoinbase { get; set; }

      public bool IsCoinstake { get; set; }

      public string LockTime { get; set; }

      public bool RBF { get; set; }

      public uint Version { get; set; }

      /// <summary>
      /// Gets or sets the transaction inputs.
      /// </summary>
      public IEnumerable<QueryTransactionInput> Inputs { get; set; }

      /// <summary>
      /// Gets or sets the transaction outputs.
      /// </summary>
      public IEnumerable<QueryTransactionOutput> Outputs { get; set; }
   }
}
