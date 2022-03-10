namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractVote
{
   public int ProposalId { get; set; }
   public string VoterAddress { get; set; }
   public bool IsApproved { get; set; }
   public long VotedOnBlock { get; set; }
}
