using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class ExecuteProposalLogReader : LogReaderBase,ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public override List<string> SupportedMethods { get; } = new() { "ExecuteProposal" };

   public override List<LogType> RequiredLogs { get; } = new() { LogType.ProposalExecutedLog };

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var log = GetLogByType(LogType.ProposalExecutedLog,contractTransaction.Logs).Log.Data;

      computedTable.CurrentAmount -= (long)log["amount"];

      string proposalId = ((long)log["proposalId"]).ToString();

      return new [] { new UpdateOneModel<DaoContractProposalTable>(Builders<DaoContractProposalTable>.Filter
            .Where(_ => _.Id.TokenId ==  proposalId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<DaoContractProposalTable>.Update
            .Set(_ => _.WasProposalAccepted,true)
            .Set(_ => _.ProposalCompletedAtBlock,contractTransaction.BlockIndex)
            .Set(_ => _.PayoutTransactionId,contractTransaction.TransactionId))};
   }
}
