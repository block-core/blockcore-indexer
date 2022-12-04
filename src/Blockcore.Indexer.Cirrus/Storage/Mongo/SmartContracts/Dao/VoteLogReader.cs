using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class VoteLogReader : ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "Vote";

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs.Any(_ => _.Log.Event == "ProposalVotedLog");

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      if (contractTransaction.Logs.All(_ => _.Log.Event != "ProposalVotedLog"))
      {
         //TODO need to handle this wierd issue (example transaction id - 5faa9c5347ba378ea1b4dd9e957e398867f724a6ba4951ed65cde6529dbfd6a0)
         return null;
      }

      int id = contractTransaction.Logs.First().Log.Data["proposalId"].ToInt32();
      bool voteYesNo = (bool)contractTransaction.Logs.First().Log.Data["vote"];
      string voterAddress = (string)contractTransaction.Logs.First().Log.Data["voter"];

      var upsertVoter = new UpdateOneModel<DaoContractProposalTable>(Builders<DaoContractProposalTable>.Filter
            .Where(_ => _.Id.ContractAddress == computedTable.ContractAddress && _.Id.TokenId == id.ToString()),
         Builders<DaoContractProposalTable>.Update.AddToSet(_ => _.Votes,
            new DaoContractVoteDetails { ProposalId = id, VoterAddress = voterAddress, PreviousVotes = new List<DaoContractVote>()}));

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
