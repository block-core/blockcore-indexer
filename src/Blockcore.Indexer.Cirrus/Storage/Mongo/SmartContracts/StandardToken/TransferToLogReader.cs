using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

public class TransferToLogReader : ILogReader<StandardTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferTo");

   public bool IsTransactionLogComplete(LogResponse[] logs) =>
      logs != null && logs.Length == 1 && logs.Single().Log.Data.Count == 3;

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenComputedTable computedTable)
   {
      if (contractTransaction.Logs == null || !contractTransaction.Logs.Any())
      {
         return; //Need to understand why this happens (example transaction id - a03a7c0122668c9c6d5a7bb47494f903a89519d8fbec2df135eb6687047c58d5)
      }

      string fromAddress = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["from"];
      string toAddress = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["to"];
      long? amount = (long?)contractTransaction.Logs.SingleOrDefault().Log.Data["amount"];

      if (fromAddress is null || toAddress is null || !amount.HasValue)
      {
         Console.WriteLine("");
      }

      var fromHolder = computedTable.TokenHolders.SingleOrDefault(_ => _.Address == fromAddress);
      fromHolder = AddStandardTokenAccountHolder(computedTable, fromHolder, fromAddress);
      fromHolder.Amount -= amount.Value;

      var toHolder = computedTable.TokenHolders.SingleOrDefault(_ => _.Address == toAddress);
      toHolder = AddStandardTokenAccountHolder(computedTable, toHolder, toAddress);

      toHolder.Amount += amount.Value;


   }

   static StandardTokenHolder AddStandardTokenAccountHolder(StandardTokenComputedTable computedTable,
      StandardTokenHolder? holder, string address)
   {
      if (holder is not null)
         return holder;

      holder = new StandardTokenHolder { Address = address };
      computedTable.TokenHolders.Add(holder);

      return holder;
   }
}
