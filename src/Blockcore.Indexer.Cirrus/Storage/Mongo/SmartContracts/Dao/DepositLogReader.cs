using System;
using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class DepositLogReader : ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType == "Deposit";

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      long amount = contractTransaction.Logs[0].Log.Data["amount"].ToInt64();

      computedTable.Deposits ??= new List<DaoContractDeposit>();

      var deposit = new DaoContractDeposit
      {
         Amount = amount,
         SenderAddress = (string)contractTransaction.Logs[0].Log.Data["sender"],
         TransactionId = contractTransaction.TransactionId,
         BlockIndex = contractTransaction.BlockIndex
      };

      computedTable.CurrentAmount += amount; //TODO sum the deposits subtract the proposals
      computedTable.Deposits.Add(deposit);

      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
