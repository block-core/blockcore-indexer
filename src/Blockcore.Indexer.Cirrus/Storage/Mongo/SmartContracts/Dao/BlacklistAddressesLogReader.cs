using System;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class BlacklistAddressesLogReader : ILogReader<DaoContractTable, DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType is "BlacklistAddresses" or "BlacklistAddress";

   public bool IsTransactionLogComplete(LogResponse[] logs) => false;

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      //TODO
      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
