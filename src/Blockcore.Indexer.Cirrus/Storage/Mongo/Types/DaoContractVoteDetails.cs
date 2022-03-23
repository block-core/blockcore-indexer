using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractVoteDetails : DaoContractVote
{
   public int ProposalId { get; set; }
   public string VoterAddress { get; set; }

   public List<DaoContractVote> PreviousVotes { get; set; }
}
