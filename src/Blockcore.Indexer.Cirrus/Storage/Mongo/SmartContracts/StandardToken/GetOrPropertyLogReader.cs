using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

class GetOrPropertyLogReader : LogReaderBase,ILogReader<StandardTokenContractTable,StandardTokenHolderTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.StartsWith("get_") ||
                                                             methodType.StartsWith("Get") ||
                                                             methodType.Equals("Allowance") ||
                                                             methodType.Equals("Approve");

   public override List<LogType> RequiredLogs { get; set; }

   public WriteModel<StandardTokenHolderTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenContractTable computedTable)
   {
      return null;
   }
}
