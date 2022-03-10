using System.Collections.Generic;
using System.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public class LogReaderFactory : ILogReaderFactory
{
   readonly List<ILogReader> readers;

   public LogReaderFactory(IEnumerable<ILogReader> readers)
   {
      this.readers = readers.ToList();
   }

   public ILogReader GetLogReader(string opCode, string methodName) => readers.FirstOrDefault(_ => _.CanReadLogForMethodType(opCode == "create" ? opCode : methodName));
}
