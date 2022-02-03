using System;
using System.Net;

namespace Blockcore.Indexer.Core.Client
{
   /// <summary>
   /// The bit net client exception.
   /// </summary>
   public class BitcoinClientException : Exception
   {
      #region Constructors and Destructors

      /// <summary>
      /// Initializes a new instance of the <see cref="BitcoinClientException"/> class.
      /// </summary>
      public BitcoinClientException(string message, Exception ex)
          : base(message, ex)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BitcoinClientException"/> class.
      /// </summary>
      public BitcoinClientException(string message)
          : base(message)
      {
      }

      #endregion

      #region Public Properties

      /// <summary>
      /// Gets or sets the error code.
      /// </summary>
      public int ErrorCode { get; set; }

      /// <summary>
      /// Gets or sets the error message.
      /// </summary>
      public string ErrorMessage { get; set; }

      /// <summary>
      /// Gets the message.
      /// </summary>
      public override string Message
      {
         get
         {
            return string.Format("StatusCode='{0}' Error={1}", StatusCode, base.Message);
         }
      }

      public string RawMessage { get; set; }

      /// <summary>
      /// Gets or sets the status code.
      /// </summary>
      public HttpStatusCode StatusCode { get; set; }

      #endregion

      #region Public Methods and Operators

      public override string ToString()
      {
         return string.Format("StatusCode = {0} Error = {1} {2}", StatusCode, RawMessage, base.ToString());
      }

      public bool IsTransactionNotFound()
      {
         if (ErrorCode == -5)
         {
            if (ErrorMessage == "No information available about transaction")
            {
               return true;
            }

            if (ErrorMessage.Contains("No such mempool or blockchain transaction"))
            {
               return true;
            }

         }

         return false;
      }

      #endregion
   }
}
