using System.Collections.Generic;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Core.Operations.Types
{
   #region Using Directives

   #endregion Using Directives

   public class StorageBatch
   {
      public long TotalSize { get; set; }
      public List<TransactionBlockTable> TransactionBlockTable { get; set; } = new List<TransactionBlockTable>();
      public Dictionary<long, BlockTable> BlockTable { get; set; } = new Dictionary<long, BlockTable>();
      public List<TransactionTable> TransactionTable { get; set; } = new List<TransactionTable>();
      public List<OutputTable> OutputTable { get; set; } = new List<OutputTable>();
      public List<InputTable> InputTable { get; set; } = new List<InputTable>();

      public object ExtraData { get; set; }
   }
}
