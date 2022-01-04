using System.Linq;

namespace Blockcore.Indexer.Operations.Types
{
   #region Using Directives

   using System.Collections.Generic;
   using Blockcore.Indexer.Storage.Mongo.Types;

   #endregion Using Directives

   public class StorageBatch
   {
      public long TotalSize { get; set; }
      public List<TransactionBlockTable> TransactionBlockTable { get; set; } = new List<TransactionBlockTable>();
      public Dictionary<long, BlockTable> BlockTable { get; set; } = new Dictionary<long, BlockTable>();
      public List<TransactionTable> TransactionTable { get; set; } = new List<TransactionTable>();
      public List<OutputTable> OutputTable { get; set; } = new List<OutputTable>();
      public List<InputTable> InputTable { get; set; } = new List<InputTable>();
   }
}
