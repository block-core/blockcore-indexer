using System;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class SetRoyaltiesLogReader : ILogReader<NonFungibleTokenContractTable,NonFungibleTokenTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType is "SetRoyalties";

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs.Length > 0;

   public WriteModel<NonFungibleTokenTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenContractTable computedTable)
   {
      //TODO the logs are empty for this method so we need to find another way to get the data

      return new WriteModel<NonFungibleTokenTable>[]{};
   }
}
