using System;

namespace Blockcore.Indexer.Core.Client
{
   #region Using Directives

   #endregion

   /// <summary>
   /// The client communication exception.
   /// </summary>
   public class BitcoinCommunicationException : Exception
   {
      #region Constructors and Destructors

      /// <summary>
      /// Initializes a new instance of the <see cref="BitcoinCommunicationException"/> class.
      /// </summary>
      public BitcoinCommunicationException(string message, Exception ex)
          : base(message, ex)
      {
      }

      #endregion
   }
}
