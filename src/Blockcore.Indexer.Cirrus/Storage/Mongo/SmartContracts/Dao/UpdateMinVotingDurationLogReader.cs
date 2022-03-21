using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMinVotingDurationLogReader : ILogReader<DaoContractComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMinVotingDuration";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      if (!contractTransaction.Logs.Any())
         return;
      computedTable.MinVotingDuration = (long)contractTransaction.Logs.Single().Log.Data["minVotingDuration"];
   }
}
