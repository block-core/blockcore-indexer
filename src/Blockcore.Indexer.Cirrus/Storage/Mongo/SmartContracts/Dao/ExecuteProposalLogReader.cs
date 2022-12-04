using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class ExecuteProposalLogReader : ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "ExecuteProposal";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var log = contractTransaction.Logs.First().Log.Data;

      computedTable.CurrentAmount -= log["amount"].ToInt64();

      string proposalId = log["proposalId"].ToString();

      return new [] { new UpdateOneModel<DaoContractProposalTable>(Builders<DaoContractProposalTable>.Filter
            .Where(_ => _.Id.TokenId ==  proposalId && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<DaoContractProposalTable>.Update
            .Set(_ => _.WasProposalAccepted,true)
            .Set(_ => _.ProposalCompletedAtBlock,contractTransaction.BlockIndex)
            .Set(_ => _.PayoutTransactionId,contractTransaction.TransactionId))};
   }
}
