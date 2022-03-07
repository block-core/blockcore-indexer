using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractProposal
{
   public int Id { get; set; }
   public string recipent { get; set; }
   public long Amount { get; set; }
   public string Description { get; set; }
   public bool WasProposalAccepted { get; set; }
   public List<DaoContractVote> Votes { get; set; }
}
