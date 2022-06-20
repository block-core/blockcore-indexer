using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMaxVotingDurationLogReader : ILogReader<DaoContractComputedTable,DaoContractProposal>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMaxVotingDuration";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<DaoContractProposal>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      var log = contractTransaction.Logs.SingleOrDefault();

      if (log == null) //TODO check if this issue persists
      {
         return Array.Empty<WriteModel<DaoContractProposal>>();;
         // throw new ArgumentException(contractTransaction.TransactionId);
      }

      computedTable.MaxVotingDuration = (long)log.Log.Data["maxVotingDuration"];

      return Array.Empty<WriteModel<DaoContractProposal>>();
   }
}
