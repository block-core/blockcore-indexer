using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Storage.Mongo.Types;

public class MongoStorageBatch : StorageBatch
{
   public long TotalSize { get; set; }
   public List<TransactionBlockTable> TransactionBlockTable { get; set; } = new();
   public Dictionary<long, BlockTable> BlockTable { get; set; } = new();
   public List<TransactionTable> TransactionTable { get; set; } = new();
   public Dictionary<string,OutputTable> OutputTable { get; set; } = new();
   public List<InputTable> InputTable { get; set; } = new();

   public override int GetBlockCount() => BlockTable.Count;

   public override int GetOutputCount() => OutputTable.Count;

   public override int GetInputCount() => InputTable.Count;

   public override int GetTransactionCount() => TransactionBlockTable.Count;

   public override long GetBatchSize() => TotalSize;

   public override IEnumerable<long> GetBlockSizes() => BlockTable.Values.Select(x => x.BlockSize).ToList();

   public override bool ValidateBatch(string prevBlockHash)
   {
      string prevHash = prevBlockHash;
      foreach (var mapBlock in BlockTable.Values.OrderBy(b => b.BlockIndex))
      {
         if (mapBlock.PreviousBlockHash != prevHash)
         {
            return false;
         }

         prevHash = mapBlock.BlockHash;
      }

      return true;
   }
}
