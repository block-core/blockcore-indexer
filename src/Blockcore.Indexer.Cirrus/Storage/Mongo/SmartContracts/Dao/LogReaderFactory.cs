using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public class LogReaderFactory<T> : ILogReaderFactory<T>
   where T : SmartContractComputedBase
{
   readonly List<ILogReader<T>> readers;

   public LogReaderFactory(IEnumerable<ILogReader<T>> readers)
   {
      this.readers = readers.ToList();
   }

   public ILogReader<T> GetLogReader(string methodName)
   {
      foreach (var reader in readers)
      {
         if (reader.CanReadLogForMethodType(methodName))
            return reader;
      }

      return null;
   }
}
