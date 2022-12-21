using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class CreateProposalLogReader : LogReaderBase,ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public override List<string> SupportedMethods { get; } = new() { "CreateProposal"};

   public override List<LogType> RequiredLogs { get; } = new() { LogType.ProposalAddedLog };

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var logData = GetLogByType(LogType.ProposalAddedLog,contractTransaction.Logs).Log.Data;

      var proposal = new DaoContractProposalTable
      {
         Recipient = logData["recipent"].ToString(),
         Amount = (long)logData["amount"],
         Id = new SmartContractTokenId
         {
            TokenId = ((long)logData["proposalId"]).ToString(),
            ContractAddress = computedTable.ContractAddress
         } ,
         Description = logData["description"].ToString(),
         ProposalStartedAtBlock = contractTransaction.BlockIndex,
         Votes = new List<DaoContractVoteDetails>()
      };

      return new [] { new InsertOneModel<DaoContractProposalTable>(proposal)};
   }
}
