using System.Text;
using ConcurrentCollections;

namespace Blockcore.Indexer.Core.Operations;

public class SlowRequestsThrottle : ISlowRequestsThrottle
{
   readonly ConcurrentHashSet<string> cache;

   public SlowRequestsThrottle(ConcurrentHashSet<string> cache)
   {
      this.cache = cache;
   }

   public bool IsRequestInProgress(string methodName, params string[] parameters)
   {
      string key = ParseKey(methodName, parameters);

      return cache.Contains(key);
   }

   public void AddRequestInProgress(string methodName, params string[] parameters)
   {
      string key = ParseKey(methodName, parameters);
      cache.Add(key);
   }

   public void RemoveCompletedRequest(string methodName, params string[] parameters)
   {
      string key = ParseKey(methodName, parameters);
      if (!cache.TryRemove(key))
         cache.Clear();
   }

   static string ParseKey(string methodName, string[] parameters)
   {
      var key = new StringBuilder(methodName);
      foreach (string parameter in parameters)
      {
         key.Append(parameter);
      }

      return key.ToString();
   }
}
