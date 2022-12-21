using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public abstract class LogReaderBase
{
   public abstract List<LogType> RequiredLogs { get; }
   public abstract List<string> SupportedMethods { get; }

   public virtual bool CanReadLogForMethodType(string methodType) => SupportedMethods is null || SupportedMethods.Contains(methodType);

   public virtual bool IsTransactionLogComplete(LogResponse[] logs)
   {
      return RequiredLogs is null || RequiredLogs.All(_ => logs.Any(l => l.Log.Event.Equals(_.ToString())));
   }

   public LogResponse GetLogByType(LogType logType, LogResponse[] logs)
   {
      return logs.FirstOrDefault(_ => _.Log.Event.Equals(logType.ToString()));
   }
}
