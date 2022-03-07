using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractTable
{
   public string ContractAddress { get; set; }

   public long CurrentAmount { get; set; }

   public List<DaoContractProposal> Proposals { get; set; }

   public List<DaoContractDeposit> Deposits { get; set; }

   public List<string> WhiteListedAddresses { get; set; }

}
