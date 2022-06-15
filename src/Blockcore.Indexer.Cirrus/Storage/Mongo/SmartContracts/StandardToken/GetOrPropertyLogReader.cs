using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

class GetOrPropertyLogReader : ILogReader<StandardTokenComputedTable,StandardTokenHolder>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.StartsWith("get_") ||
                                                             methodType.StartsWith("Get") ||
                                                             methodType.Equals("Allowance") ||
                                                             methodType.Equals("Approve");

   public bool IsTransactionLogComplete(LogResponse[] logs) => true;

   public WriteModel<StandardTokenHolder>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenComputedTable computedTable)
   {
      return null;
   }
}
