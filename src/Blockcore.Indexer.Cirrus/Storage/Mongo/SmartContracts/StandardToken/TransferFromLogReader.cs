using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

public class TransferFromLogReader : ILogReader<StandardTokenComputedTable,StandardTokenHolder>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<StandardTokenHolder>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenComputedTable computedTable)
   {
      string fromAddress = (string)contractTransaction.Logs.Single().Log.Data["from"];
      string toAddress = (string)contractTransaction.Logs.Single().Log.Data["to"];
      long amount = (long)contractTransaction.Logs.Single().Log.Data["amount"];

      var fromHolder = computedTable.TokenHolders.Single(_ => _.Address == fromAddress);
      fromHolder.Amount -= amount;

      var toHolder = computedTable.TokenHolders.SingleOrDefault(_ => _.Address == toAddress);
      if (toHolder is null)
      {
         toHolder = new StandardTokenHolder { Address = toAddress };
         computedTable.TokenHolders.Add(toHolder);
      }

      toHolder.Amount += amount;

      return new[]
      {
         new UpdateOneModel<StandardTokenHolder>(Builders<StandardTokenHolder>.Filter
               .Where(_ => _.Address == fromAddress),
            Builders<StandardTokenHolder>.Update.Inc(_ => _.Amount, -amount)),
         new UpdateOneModel<StandardTokenHolder>(Builders<StandardTokenHolder>.Filter
               .Where(_ => _.Address == toAddress),
            Builders<StandardTokenHolder>.Update.Inc(_ => _.Amount, amount))
      };
   }
}
