using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class DepositLogReader : ILogReader
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "Deposit";

   public bool IsTheTransactionLogComplete(LogResponse[] logs) => true;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      long amount = (long)contractTransaction.Logs[0].Log.Data["amount"];

      computedTable.Deposits ??= new List<DaoContractDeposit>();

      computedTable.Deposits.Add(new DaoContractDeposit
      {
         Amount = amount,
         SenderAddress = (string)contractTransaction.Logs[0].Log.Data["sender"]
      });

      computedTable.CurrentAmount += amount; //TODO sum the deposits subtract the proposals
   }
}
