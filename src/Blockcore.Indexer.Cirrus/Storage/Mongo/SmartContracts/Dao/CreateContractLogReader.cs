using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public class CreateContractLogReader : ILogReader
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "create";

   public bool IsTheTransactionLogComplete(LogResponse[] logs) => false;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,DaoContractComputedTable computedTable)
   {
      computedTable.ContractAddress = contractTransaction.NewContractAddress;
   }
}
