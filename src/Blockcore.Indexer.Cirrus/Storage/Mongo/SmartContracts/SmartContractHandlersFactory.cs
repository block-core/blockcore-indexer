using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public class SmartContractHandlersFactory<T,TDocument> : ISmartContractHandlersFactory<T,TDocument>
   where T : SmartContractTable
   where TDocument : new()
{
   readonly IEnumerable<ISmartContractBuilder<T>> builders;
   readonly List<ILogReader<T,TDocument>> readers;

   public SmartContractHandlersFactory(IEnumerable<ILogReader<T,TDocument>> readers, IEnumerable<ISmartContractBuilder<T>> builders)
   {
      this.builders = builders;
      this.readers = readers.ToList();
   }

   public ILogReader<T, TDocument> GetLogReader(string methodName)
   {
      foreach (var reader in readers)
      {
         if (reader.CanReadLogForMethodType(methodName))
            return reader;
      }

      return null;
   }

   public ISmartContractBuilder<T> GetSmartContractBuilder(string contractType)
   {
      return builders.FirstOrDefault(_ => _.CanBuildSmartContract(contractType)); //this is not a hot path so we can use Linq
   }
}
