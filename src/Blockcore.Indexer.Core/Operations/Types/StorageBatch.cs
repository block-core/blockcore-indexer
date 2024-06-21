using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Operations.Types
{
   #region Using Directives

   #endregion Using Directives

   public abstract class StorageBatch
   {
      public abstract int GetBlockCount();
      public abstract int GetOutputCount();
      public abstract int GetInputCount();
      public abstract int GetTransactionCount();
      public abstract long GetBatchSize();

      public abstract IEnumerable<long> GetBlockSizes();
      public abstract bool ValidateBatch(string prevBlockHash);
   }
}
