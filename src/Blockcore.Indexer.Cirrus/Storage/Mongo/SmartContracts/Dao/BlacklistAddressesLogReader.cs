using System;
using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class BlacklistAddressesLogReader : LogReaderBase,ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public override List<LogType> RequiredLogs { get; }
   public override List<string> SupportedMethods { get; } = new() { "BlacklistAddresses", "BlacklistAddress" };

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      //TODO
      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
