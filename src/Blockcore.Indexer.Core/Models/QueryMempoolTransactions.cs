namespace Blockcore.Indexer.Api.Handlers.Types
{
   using System.Collections.Generic;

   public class QueryMempoolTransaction
   {
      public string TransactionId { get; set; }
   }

   public class QueryMempoolTransactions
   {
      public string CoinTag { get; set; }

      public IEnumerable<QueryMempoolTransaction> Transactions { get; set; }
   }
}
