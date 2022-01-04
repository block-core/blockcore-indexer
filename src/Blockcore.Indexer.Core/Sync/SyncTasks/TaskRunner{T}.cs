namespace Blockcore.Indexer.Sync.SyncTasks
{
   using System;
   using System.Collections.Concurrent;
   using System.Linq;
   using Blockcore.Indexer.Settings;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;

   public abstract class TaskRunner<T> : TaskRunner, IBlockableItem
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="TaskRunner{T}"/> class.
      /// </summary>
      protected TaskRunner(IOptions<IndexerSettings> configuration, ILogger logger)
          : base(configuration, logger)
      {
         Queue = new ConcurrentQueue<T>();
      }

      public ConcurrentQueue<T> Queue { get; set; }

      public bool Blocked { get; set; }

      public void Enqueue(T item)
      {
         if (!Blocked)
         {
            Queue.Enqueue(item);
         }
      }

      public bool TryDequeue(out T result)
      {
         if (!Blocked)
         {
            return Queue.TryDequeue(out result);
         }

         result = default(T);
         return false;
      }

      public void Deplete()
      {
         while (Queue.TryDequeue(out T item))
         {
            // Do nothing.
         }

         if (Queue.Any())
         {
            throw new Exception("Failed to empty queue.");
         }
      }
   }
}
