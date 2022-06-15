using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class DepositLogReader : ILogReader<DaoContractComputedTable,DaoContractDeposit>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "Deposit";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<DaoContractDeposit>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractComputedTable computedTable)
   {
      long amount = (long)contractTransaction.Logs[0].Log.Data["amount"];

      computedTable.Deposits ??= new List<DaoContractDeposit>();

      var deposit = new DaoContractDeposit
      {
         Amount = amount,
         SenderAddress = (string)contractTransaction.Logs[0].Log.Data["sender"],
         TransactionId = contractTransaction.TransactionId
      };

      computedTable.CurrentAmount += amount; //TODO sum the deposits subtract the proposals

      return new [] { new InsertOneModel<DaoContractDeposit>(deposit)};
   }
}
