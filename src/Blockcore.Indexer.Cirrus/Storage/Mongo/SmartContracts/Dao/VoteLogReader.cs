using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class VoteLogReader : LogReaderBase, ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public override List<string> SupportedMethods { get; } = new() { "Vote" };
   public override List<LogType> RequiredLogs { get; } = new() { LogType.ProposalVotedLog };

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(
      CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var proposalLog = GetLogByType(LogType.ProposalVotedLog, contractTransaction.Logs);

      if (proposalLog == null)
      {
         //TODO need to handle this wierd issue (example transaction id - 5faa9c5347ba378ea1b4dd9e957e398867f724a6ba4951ed65cde6529dbfd6a0)
         return null;
      }

      int id = (int)(long)proposalLog.Log.Data["proposalId"];
      bool voteYesNo = (bool)proposalLog.Log.Data["vote"];
      string voterAddress = proposalLog.Log.Data["voter"].ToString();

      var upsertVoter = new UpdateOneModel<DaoContractProposalTable>(Builders<DaoContractProposalTable>.Filter
            .Where(_ => _.Id.ContractAddress == computedTable.ContractAddress && _.Id.TokenId == id.ToString()),
         Builders<DaoContractProposalTable>.Update.AddToSet(_ => _.Votes,
            new DaoContractVoteDetails
            {
               ProposalId = id, VoterAddress = voterAddress, PreviousVotes = new List<DaoContractVote>()
            }));

      var insertVoteForVoter = new UpdateOneModel<DaoContractProposalTable>(Builders<DaoContractProposalTable>.Filter
            .Where(_ => _.Id.ContractAddress == computedTable.ContractAddress && _.Id.TokenId == id.ToString()),
         Builders<DaoContractProposalTable>.Update.AddToSet("Votes.$[j].PreviousVotes",
            new DaoContractVote { IsApproved = voteYesNo, VotedOnBlock = contractTransaction.BlockIndex }));

      insertVoteForVoter.ArrayFilters = new[]
      {
         new BsonDocumentArrayFilterDefinition<DaoContractVoteDetails>(
            new BsonDocument("j.VoterAddress", voterAddress))
      };

      return new[] { upsertVoter, insertVoteForVoter };
   }
}
