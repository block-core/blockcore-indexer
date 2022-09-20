using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.OPDEX;

public class DistributeGenesisLogReader : ILogReader<OpdexMinedTokenContractTable,OpdexMIndTokenHolderTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("DistributeGenesis");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs.Any(_ => _.Log.Event.Equals("DistributionLog"));

   public WriteModel<OpdexMIndTokenHolderTable>[] UpdateContractFromTransactionLog(
      CirrusContractTable contractTransaction,
      OpdexMinedTokenContractTable computedTable)
   {
      var log = contractTransaction.Logs.First(_ => _.Log.Event.Equals("DistributionLog"));

      return new WriteModel<OpdexMIndTokenHolderTable>[]{};
   }
}
