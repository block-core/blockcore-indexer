using System;
using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class DepositLogReader : LogReaderBase,ILogReader<DaoContractTable,DaoContractProposalTable>
{
   public override List<string> SupportedMethods { get; } = new() { "Deposit" };

   public override List<LogType> RequiredLogs { get; } = new() { LogType.FundRaisedLog };

   public WriteModel<DaoContractProposalTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      DaoContractTable computedTable)
   {
      var fundRaisedLog = GetLogByType(LogType.FundRaisedLog, contractTransaction.Logs);

      long amount = (long)fundRaisedLog.Log.Data["amount"];

      computedTable.Deposits ??= new List<DaoContractDeposit>();

      var deposit = new DaoContractDeposit
      {
         Amount = amount,
         SenderAddress = fundRaisedLog.Log.Data["sender"].ToString(),
         TransactionId = contractTransaction.TransactionId,
         BlockIndex = contractTransaction.BlockIndex
      };

      computedTable.CurrentAmount += amount; //TODO sum the deposits subtract the proposals
      computedTable.Deposits.Add(deposit);

      return Array.Empty<WriteModel<DaoContractProposalTable>>();
   }
}
