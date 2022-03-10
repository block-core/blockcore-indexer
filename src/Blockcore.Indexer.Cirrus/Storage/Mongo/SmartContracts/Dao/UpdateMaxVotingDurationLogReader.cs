using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMaxVotingDurationLogReader : ILogReader
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMaxVotingDuration";

   public bool IsTheTransactionLogComplete(LogResponse[] logs)
   {
      return false;
   }

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      computedTable.MaxVotingDuration = (long)contractTransaction.Logs.Single().Log.Data["maxVotingDuration"];
   }
}
