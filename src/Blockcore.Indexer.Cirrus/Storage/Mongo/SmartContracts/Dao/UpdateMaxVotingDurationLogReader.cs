using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMaxVotingDurationLogReader : ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMaxVotingDuration";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var log = contractTransaction.Logs.SingleOrDefault();

      if (log == null) //TODO check if this issue persists
      {
         return Array.Empty<WriteModel<DaoContractProposalTable>>();
         // throw new ArgumentException(contractTransaction.TransactionId);
      }

      computedTable.MaxVotingDuration = (long)log.Log.Data["maxVotingDuration"];

      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
