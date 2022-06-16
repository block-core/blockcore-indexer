using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class ExecuteProposalLogReader : ILogReader<DaoContractComputedTable, DaoContractProposal>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "ExecuteProposal";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<DaoContractProposal>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      var log = contractTransaction.Logs.First().Log.Data;

      computedTable.CurrentAmount -= (long)log["amount"];

      string proposalId = ((long)log["proposalId"]).ToString();

      return new [] { new UpdateOneModel<DaoContractProposal>(Builders<DaoContractProposal>.Filter
            .Where(_ => _.Id.TokenId ==  proposalId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<DaoContractProposal>.Update
            .Set(_ => _.WasProposalAccepted,true)
            .Set(_ => _.ProposalCompletedAtBlock,contractTransaction.BlockIndex)
            .Set(_ => _.PayoutTransactionId,contractTransaction.TransactionId))};
   }
}
