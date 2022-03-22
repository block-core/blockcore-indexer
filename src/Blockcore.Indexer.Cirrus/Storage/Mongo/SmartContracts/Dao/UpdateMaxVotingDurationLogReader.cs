using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMaxVotingDurationLogReader : ILogReader
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMaxVotingDuration";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      var log = contractTransaction.Logs.SingleOrDefault();

      if (log == null)
      {
         return;
         // throw new ArgumentException(contractTransaction.TransactionId);
      }

      computedTable.MaxVotingDuration = (long)log.Log.Data["maxVotingDuration"];
   }
}
