using System;

namespace Blockcore.Indexer.Core.Sync
{
   public class SyncRestartException : Exception
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="SyncRestartException"/> class.
      /// </summary>
      public SyncRestartException()
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="SyncRestartException"/> class.
      /// </summary>
      public SyncRestartException(string message)
          : base(message)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="SyncRestartException"/> class.
      /// </summary>
      public SyncRestartException(string message, Exception ex)
          : base(message, ex)
      {
      }
   }
}
