namespace Blockcore.Indexer.Storage
{
   using System;
   using System.Collections.Generic;
   using Blockcore.Indexer.Api.Handlers.Types;
   using Blockcore.Indexer.Storage.Mongo;
   using Blockcore.Indexer.Storage.Mongo.Types;
   using Blockcore.Indexer.Storage.Types;
   using NBitcoin;

   public interface IStorage
   {
      SyncBlockInfo GetLatestBlock();

      int GetMemoryTransactionsCount();

      AddressBalance AddressBalance(string address, long confirmations = 0, bool includeMempool = false);

      QueryResult<QueryTransaction> AddressTransactions(string address, long confirmations, bool unconfirmed, TransactionUsedFilter used, int offset, int limit);

      QueryResult<QueryTransaction> GetMemoryTransactions(int offset, int limit);

      QueryTransaction GetTransaction(string transactionId);

      QueryResult<SyncTransactionInfo> TransactionsByBlock(string hash, int offset, int limit);

      QueryResult<SyncTransactionInfo> TransactionsByBlock(long index, int offset, int limit);

      QueryResult<SyncBlockInfo> Blocks(int offset, int limit);

      SyncBlockInfo BlockByHash(string blockHash);

      SyncBlockInfo BlockByIndex(long blockIndex);

      QueryResult<MapRichlist> Richlist(int offset, int limit);

      MapRichlist RichlistBalance(string address);

      List<MapRichlist> AddressBalances(IEnumerable<string> addresses);

      long TotalBalance();

      //IEnumerable<SyncBlockInfo> BlockGetCompleteBlockCount(int count);

      //SyncBlockInfo BlockByHash(string blockHash);

      //SyncTransactionInfo BlockTransactionGet(string transactionId);

      //(IEnumerable<SyncBlockInfo> Items, int Total) BlockGetByLimitOffset(int offset, int limit);

      //IEnumerable<SyncTransactionInfo> BlockTransactionGetByBlock(string blockHash);

      //IEnumerable<SyncTransactionInfo> BlockTransactionGetByBlockIndex(long blockIndex);

      //SyncTransactionItemOutput TransactionsGet(string transactionId, int index, SyncTransactionIndexType indexType);

      //SyncTransactionItems TransactionItemsGet(string transactionId);

      //Address AddressGetBalance(string address, long confirmations, bool availableOnly);

      //// Address AddressGetBalanceUtxo(string address, long confirmations);

      //string GetSpendingTransaction(string transaction, int index);

      void DeleteBlock(string blockHash);

      //IEnumerable<NBitcoin.Transaction> GetMemoryTransactions();
   }
}
