using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Models
{
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
