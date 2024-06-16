using System.Collections.Generic;
using Blockcore.Indexer.Core.Storage.Postgres.Types;

namespace Blockcore.Indexer.Core.Operations.Types
{
   #region Using Directives

   #endregion Using Directives

   public class StorageBatch
   {
      public long TotalSize { get; set; }
      // public List<TransactionBlockTable> TransactionBlockTable { get; set; } = new();
      // public Dictionary<long, BlockTable> BlockTable { get; set; } = new();
      // public List<TransactionTable> TransactionTable { get; set; } = new();
      public List<Input> Inputs { get; set; } = [];
      public Dictionary<long, Block> Blocks { get; set; } = [];
      public Dictionary<string,Output> Outputs { get; set; } = [];

      public object ExtraData { get; set; }
   }
}
