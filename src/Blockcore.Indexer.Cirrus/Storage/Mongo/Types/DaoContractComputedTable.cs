using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractComputedTable : SmartContractComputedBase
{
   public override string ContractType { get; } = "DAOContract";

   public long CurrentAmount { get; set; }
   public long MaxVotingDuration { get; set; }
   public long MinVotingDuration { get; set; }
   public long WhitelistedCount { get; set; }

   public List<DaoContractProposal> Proposals { get; set; }

   public List<DaoContractDeposit> Deposits { get; set; }

   public List<string> ApprovedAddresses { get; set; }

}
