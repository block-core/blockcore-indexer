
namespace Blockcore.Indexer.Storage
{
   using System.Collections.Generic;
   using Blockcore.Indexer.Storage.Types;

   public interface IStorage
   {
      IEnumerable<SyncBlockInfo> BlockGetIncompleteBlocks();

      IEnumerable<SyncBlockInfo> BlockGetBlockCount(int count);

      IEnumerable<SyncBlockInfo> BlockGetCompleteBlockCount(int count);

      SyncBlockInfo BlockGetByHash(string blockHash);

      SyncBlockInfo BlockGetByIndex(long blockIndex);

      SyncTransactionInfo BlockTransactionGet(string transactionId);

      (IEnumerable<SyncBlockInfo> Items, int Total) BlockGetByLimitOffset(int offset, int limit);

      IEnumerable<SyncTransactionInfo> BlockTransactionGetByBlock(string blockHash);

      IEnumerable<SyncTransactionInfo> BlockTransactionGetByBlockIndex(long blockIndex);

      SyncTransactionItemOutput TransactionsGet(string transactionId, int index, SyncTransactionIndexType indexType);

      SyncTransactionItems TransactionItemsGet(string transactionId);

      SyncTransactionAddressBalance AddressGetBalance(string address, long confirmations);

      SyncTransactionAddressBalance AddressGetBalanceUtxo(string address, long confirmations);

      string GetSpendingTransaction(string transaction, int index);

      void DeleteBlock(string blockHash);

      IEnumerable<NBitcoin.Transaction> GetMemoryTransactions();
   }
}

