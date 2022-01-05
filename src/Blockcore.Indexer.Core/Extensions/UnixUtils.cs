using System;

namespace Blockcore.Indexer.Core.Extensions
{
   /// <summary>
   /// Internal class providing certain utility functions to other classes.
   /// </summary>
   internal sealed class UnixUtils
   {
      /// <summary>
      /// The Unix start date.
      /// </summary>
      private static readonly DateTime UnixStartDate = new DateTime(1970, 1, 1, 0, 0, 0);

      /// <summary>
      /// Converts a <see cref="DateTime"/> object into a unix timestamp number.
      /// </summary>
      /// <param name="date">
      /// The date to convert.
      /// </param>
      /// <returns>
      /// A long for the number of seconds since 1st January 1970, as per unix specification.
      /// </returns>
      internal static long DateToUnixTimestamp(DateTime date)
      {
         TimeSpan ts = date - UnixStartDate;
         return (long)ts.TotalSeconds;
      }

      /// <summary>
      /// Converts a string, representing a unix timestamp number into a <see cref="DateTime"/> object.
      /// </summary>
      /// <param name="timestamp">
      /// The timestamp, as a string.
      /// </param>
      /// <returns>
      /// The <see cref="DateTime"/> object the time represents.
      /// </returns>
      internal static DateTime UnixTimestampToDate(string timestamp)
      {
         if (string.IsNullOrEmpty(timestamp))
         {
            return DateTime.MinValue;
         }

         return UnixTimestampToDate(long.Parse(timestamp));
      }

      /// <summary>
      /// Converts a <see cref="long"/>, representing a unix timestamp number into a <see cref="DateTime"/> object.
      /// </summary>
      /// <param name="timestamp">
      /// The unix timestamp.
      /// </param>
      /// <returns>
      /// The <see cref="DateTime"/> object the time represents.
      /// </returns>
      internal static DateTime UnixTimestampToDate(long timestamp)
      {
         return UnixStartDate.AddSeconds(timestamp);
      }
   }
}
