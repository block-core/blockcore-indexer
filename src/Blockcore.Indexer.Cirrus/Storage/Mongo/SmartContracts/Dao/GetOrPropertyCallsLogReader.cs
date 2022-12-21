using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public class GetOrPropertyCallsLogReader : LogReaderBase,ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public override List<string> SupportedMethods { get; }
   public override List<LogType> RequiredLogs { get; }
   public bool CanReadLogForMethodType(string methodType) => methodType.StartsWith("get_") ||
                                                             methodType.StartsWith("Get") ||
                                                             methodType == "IsWhitelisted";

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      return null;
   }
}
