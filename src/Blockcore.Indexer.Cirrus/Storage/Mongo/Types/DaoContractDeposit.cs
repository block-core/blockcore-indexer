namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class DaoContractDeposit
{
   public string SenderAddress { get; set; }
   public long Amount { get; set; }

   public string TransactionId { get; set; }
   public long BlockIndex { get; set; }
}
