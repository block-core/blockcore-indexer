namespace Blockcore.Indexer.Core.Extensions
{
   public class Stopwatch
   {
      public static System.Diagnostics.Stopwatch Start()
      {
         var stoper = new System.Diagnostics.Stopwatch();
         stoper.Start();
         return stoper;
      }
   }
}
