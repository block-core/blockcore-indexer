using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class UpdateMinVotingDurationLogReader : ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "UpdateMinVotingDuration";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      if (!contractTransaction.Logs.Any())
         return null;
      computedTable.MinVotingDuration = (long)contractTransaction.Logs.Single().Log.Data["minVotingDuration"];

      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
