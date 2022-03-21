using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public class LogReaderFactory : ILogReaderFactory
{
   readonly List<ILogReader<SmartContractComputedBase>> readers;

   public LogReaderFactory(IEnumerable<ILogReader<SmartContractComputedBase>> readers)
   {
      this.readers = readers.ToList();
   }

   public ILogReader<Tconcrete> GetLogReader<Tconcrete>(string methodName)
      where Tconcrete : SmartContractComputedBase
   {
      foreach (var reader in readers)
      {
         if (reader.CanReadLogForMethodType(methodName))
            return reader;
      }

      return null;
   }
}
