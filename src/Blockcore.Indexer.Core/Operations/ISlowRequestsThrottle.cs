namespace Blockcore.Indexer.Core.Operations;

public interface ISlowRequestsThrottle
{
   bool IsRequestInProgress(string methodName, params string[] parameters);
   void AddRequestInProgress(string methodName, params string[] parameters);
   void RemoveCompletedRequest(string methodName, params string[] parameters);
}
