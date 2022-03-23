using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractProposal
{
   public int Id { get; set; }
   public string Recipient { get; set; }
   public long Amount { get; set; }
   public string Description { get; set; }
   public bool WasProposalAccepted { get; set; }
   public long ProposalStartedAtBlock { get; set; }
   public long ProposalCompletedAtBlock { get; set; }
   public string PayoutTransactionId { get; set; }
   public List<DaoContractVoteDetails> Votes { get; set; }
}
