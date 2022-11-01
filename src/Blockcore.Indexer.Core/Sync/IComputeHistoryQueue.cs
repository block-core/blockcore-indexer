namespace Blockcore.Indexer.Core.Sync;

public interface IComputeHistoryQueue
{
   bool IsQueueEmpty();
   void AddAddressToComputeHistoryQueue(string address);
   bool GetNextItemFromQueue(out string address);
}
