using System.Collections.Concurrent;
using System.Linq;
using Blockcore.Indexer.Core.Settings;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync;

public class ComputeHistoryQueue : IComputeHistoryQueue
{
   readonly ConcurrentQueue<string> collection;
   readonly IndexerSettings indexerSettings;

   public ComputeHistoryQueue(IOptions<IndexerSettings> indexerSettings)
   {
      this.indexerSettings = indexerSettings.Value;
      collection = new ConcurrentQueue<string>();
   }

   public bool IsQueueEmpty() => collection.IsEmpty;

   public void AddAddressToComputeHistoryQueue(string address)
   {
      if (indexerSettings.MaxItemsInHistoryQueue <= 0 || collection.Count >= indexerSettings.MaxItemsInHistoryQueue)
      {
         return;
      }

      if (!collection.Contains(address))
      {
         collection.Enqueue(address);
      }
   }

   public bool GetNextItemFromQueue(out string address)
   {
      address = null;
      return !collection.IsEmpty && collection.TryDequeue(out address);
   }
}
