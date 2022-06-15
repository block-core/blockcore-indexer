using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class WhitelistAddressesLogReader : ILogReader<DaoContractComputedTable,DaoContractProposal>
{
   public bool CanReadLogForMethodType(string methodType) => methodType is "WhitelistAddresses" or "WhitelistAddress";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<DaoContractProposal>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      computedTable.WhitelistedCount =  (long)contractTransaction.Logs.Single().Log.Data["whitelistedCount"];
      //TODO we don't have the address or a way to get it without reading the script in the transaction

      return new WriteModel<DaoContractProposal>[]{};
   }
}
