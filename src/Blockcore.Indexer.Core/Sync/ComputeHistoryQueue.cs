using System.Collections.Concurrent;
using System.Linq;

namespace Blockcore.Indexer.Core.Sync;

public class ComputeHistoryQueue : IComputeHistoryQueue
{
   readonly ConcurrentQueue<string> collection;

   public ComputeHistoryQueue()
   {
      collection = new ConcurrentQueue<string>();
   }

   public bool IsQueueEmpty() => collection.IsEmpty;

   public void AddAddressToComputeHistoryQueue(string address)
   {
      if (!collection.Contains(address))
         collection.Enqueue(address);
   }

   public bool GetNextItemFromQueue(out string address)
   {
      address = null;
      return !collection.IsEmpty && collection.TryDequeue(out address);
   }
}
