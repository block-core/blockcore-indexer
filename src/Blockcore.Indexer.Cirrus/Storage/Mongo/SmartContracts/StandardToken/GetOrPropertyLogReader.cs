using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

class GetOrPropertyLogReader : LogReaderBase,ILogReader<StandardTokenContractTable,StandardTokenHolderTable>
{
   public override List<string> SupportedMethods { get; }
   public override List<LogType> RequiredLogs { get; }

   public bool CanReadLogForMethodType(string methodType) => methodType.StartsWith("get_") ||
                                                             methodType.StartsWith("Get") ||
                                                             methodType.Equals("Allowance") ||
                                                             methodType.Equals("Approve");

   public WriteModel<StandardTokenHolderTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenContractTable computedTable)
   {
      return null;
   }
}
