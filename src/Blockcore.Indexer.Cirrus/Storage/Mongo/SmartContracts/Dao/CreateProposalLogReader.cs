using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class CreateProposalLogReader : ILogReader<DaoContractComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "CreateProposal";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      var logData = contractTransaction.Logs.First().Log.Data;

      var proposal = new DaoContractProposal
      {
         Recipient = (string)logData["recipent"],
         Amount = (long)logData["amount"],
         Id = (int)(long)logData["proposalId"],
         Description = (string)logData["description"],
         ProposalStartedAtBlock = contractTransaction.BlockIndex,
         Votes = new List<DaoContractVoteDetails>()
      };

      computedTable.Proposals ??= new List<DaoContractProposal>(proposal.Id);

      if (computedTable.Proposals.Capacity < proposal.Id)
         computedTable.Proposals.Capacity = (proposal.Id);

      computedTable.Proposals.Insert(proposal.Id - 1,proposal);
   }
}
