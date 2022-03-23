namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractVote
{
   public bool IsApproved { get; set; }
   public long VotedOnBlock { get; set; }
}
