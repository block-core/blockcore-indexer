using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public interface IContractDataReader<in T> where T : SmartContractComputedBase
{
   bool CanReadLogForMethodType(string methodType);
   bool IsTransactionLogComplete(IDictionary<string,object> dictionary);
   UpdateDefinition<TToken> UpdateContractFromTransactionLog<TToken>(CirrusContractTable contractTransaction,T computedTable);
}

class ContractDataReader : IContractDataReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType)  => methodType.Equals("Burn");

   public bool IsTransactionLogComplete(IDictionary<string, object> dictionary) => dictionary.ContainsKey("Burn");

   public UpdateDefinition<Token> UpdateContractFromTransactionLog<Token>(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      return null;
   }
}
