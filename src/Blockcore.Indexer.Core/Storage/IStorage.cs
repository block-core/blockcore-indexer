using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Models;

using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage
{
   public interface IStorage
   {
      SyncBlockInfo GetLatestBlock();

      int GetMemoryTransactionsCount();

      QueryAddress AddressBalance(string address);

      AddressResponse AddressResponseBalance(string address);

      Task<List<QueryAddressBalance>> QuickBalancesLookupForAddressesWithHistoryCheckAsync(
         IEnumerable<string> addresses, bool includePending = false);

      QueryResult<QueryAddressItem> AddressHistory(string address, int? offset, int limit);

      List<MempoolTransaction> GetMempoolTransactionList(List<string> txids);

      QueryResult<QueryMempoolTransactionHashes> GetMemoryTransactionsSlim(int offset, int limit);

      QueryResult<QueryTransaction> GetMemoryTransactions(int offset, int limit);

      string GetRawTransaction(string transactionId);

      QueryTransaction GetTransaction(string transactionId);

      QueryResult<SyncTransactionInfo> TransactionsByBlock(string hash, int offset, int limit);

      QueryResult<SyncTransactionInfo> TransactionsByBlock(long index, int offset, int limit);

      QueryResult<SyncBlockInfo> Blocks(int? offset, int limit);

      SyncBlockInfo BlockByHash(string blockHash);

      string GetRawBlock(string blockHash);

      SyncBlockInfo BlockByIndex(long blockIndex);

      QueryResult<QueryOrphanBlock> OrphanBlocks(int? offset, int limit);

      T OrphanBlockByHash<T>(string blockHash) where T : class;

      QueryResult<BalanceForAddress> Richlist(int offset, int limit);

      List<BalanceForAddress> AddressBalances(IEnumerable<string> addresses);

      long TotalBalance();

      Task<QueryResult<Output>> GetUnspentTransactionsByAddressAsync(string address, long confirmations, int offset, int limit);

      Task DeleteBlockAsync(string blockHash);

      public List<string> GetBlockIndexIndexes();

      public List<string> GetMempoolTransactionIds();

      public bool DeleteTransactionsFromMempool(List<string> transactionIds);

      List<PeerDetails> GetPeerFromDate(DateTime date);
      Task<long> InsertPeer(PeerDetails info);
      
      public Output GetOutputFromOutpoint(string txid, int index);
      
      public List<OutspentResponse> GetTransactionOutspends(string txid);
   }
}
