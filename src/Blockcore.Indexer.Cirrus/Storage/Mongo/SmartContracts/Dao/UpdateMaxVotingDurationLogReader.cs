using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMaxVotingDurationLogReader : LogReaderBase,ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMaxVotingDuration";

   public override List<LogType> RequiredLogs { get; set; }
   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var log = contractTransaction.Logs.SingleOrDefault();

      if (log == null) //TODO check if this issue persists
      {
         return Array.Empty<WriteModel<DaoContractProposalTable>>();
      }

      computedTable.MaxVotingDuration = (long)log.Log.Data["maxVotingDuration"]; //TODO add logic to update the smart contract table

      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
