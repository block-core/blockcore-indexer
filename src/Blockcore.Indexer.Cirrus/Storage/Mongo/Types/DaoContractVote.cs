namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractVote
{
   public int ProposalId { get; set; }
   public string VoterAddress { get; set; }
   public bool Decision { get; set; }
}
