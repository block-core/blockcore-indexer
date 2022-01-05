using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Storage.Types
{
   public class QueryResult<T>
   {
      public int Offset { get; set; }

      public int Limit { get; set; }

      public long Total { get; set; }

      public IEnumerable<T> Items { get; set; }
   }
}
