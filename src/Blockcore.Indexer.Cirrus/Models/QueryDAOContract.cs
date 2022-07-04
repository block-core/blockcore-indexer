using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Models;

public class QueryDAOContract
{
   public long CurrentAmount { get; set; }
   public long MaxVotingDuration { get; set; }
   public long MinVotingDuration { get; set; }
   public long WhitelistedCount { get; set; }
   public List<DaoContractDeposit> Deposits { get; set; }
   public List<DaoContractProposalTable> Proposals { get; set; }
   public List<string> ApprovedAddresses { get; set; }

}
