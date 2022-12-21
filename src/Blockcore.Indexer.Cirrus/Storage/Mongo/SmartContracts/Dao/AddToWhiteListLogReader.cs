using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class WhitelistAddressesLogReader : LogReaderBase,ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public override List<LogType> RequiredLogs { get; set; }

   public bool CanReadLogForMethodType(string methodType) => methodType is "WhitelistAddresses" or "WhitelistAddress";

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      computedTable.WhitelistedCount =  (long)contractTransaction.Logs.Single().Log.Data["whitelistedCount"];
      //TODO we don't have the address or a way to get it without reading the script in the transaction

      return new WriteModel<DaoContractProposalTable>[]{};
   }
}
