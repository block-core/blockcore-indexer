using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Sync
{
   /// <summary>
   /// The CoinOperations interface.
   /// </summary>
   public class SyncOperations : ISyncOperations
   {
      private readonly IStorage storage;

      private readonly ILogger<SyncOperations> log;

      private readonly IndexerSettings configuration;

      private readonly IMemoryCache cache;
      readonly GlobalState globalState;

      private readonly MemoryCacheEntryOptions cacheOptions;

      readonly ICryptoClientFactory clientFactory;

      readonly ISyncBlockTransactionOperationBuilder transactionOperationBuilder;

      /// <summary>
      /// Initializes a new instance of the <see cref="SyncOperations"/> class.
      /// </summary>
      public SyncOperations(
         IStorage storage,
         ILogger<SyncOperations> logger,
         IOptions<IndexerSettings> configuration,
         IMemoryCache cache,
         GlobalState globalState, ICryptoClientFactory clientFactory,
         ISyncBlockTransactionOperationBuilder blockInfoEnrichment)
      {
         this.configuration = configuration.Value;
         log = logger;
         this.storage = storage;
         this.cache = cache;
         this.globalState = globalState;
         this.clientFactory = clientFactory;
         transactionOperationBuilder = blockInfoEnrichment;

         // Register the cold staking template.
         StandardScripts.RegisterStandardScriptTemplate(ColdStakingScriptTemplate.Instance);

         cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheKeys.BlockCountTime);
      }

      public void InitializeMmpool()
      {
         var mempoolTransactionIds = storage.GetMempoolTransactionIds();

         foreach (var transactionId in mempoolTransactionIds)
         {
            globalState.LocalMempoolView.TryAdd(transactionId, string.Empty);
         }
      }

      public long GetBlockCount(IBlockchainClient client)
      {
         if (!cache.TryGetValue(CacheKeys.BlockCount, out long cacheEntry))
         {
            cacheEntry = client.GetBlockCount();

            // Save data in cache.
            cache.Set(CacheKeys.BlockCount, cacheEntry, cacheOptions);
         }

         return cacheEntry;
      }

      public SyncPoolTransactions FindPoolTransactions(SyncConnection connection)
      {
         return FindPoolInternal(connection);
      }

      public SyncBlockTransactionsOperation SyncPool(SyncConnection connection, SyncPoolTransactions poolTransactions)
      {
         return SyncPoolInternal(connection, poolTransactions);
      }

      public SyncBlockTransactionsOperation FetchFullBlock(SyncConnection connection, BlockInfo block)
      {
         return SyncBlockInternal(connection, block);
      }

      public async Task<SyncBlockInfo> RewindToBestChain(SyncConnection connection)
      {
         IBlockchainClient client = clientFactory.Create(connection);

         while (true)
         {
            SyncBlockInfo block = storage.GetLatestBlock();

            if (block == null)
            {
               return null;
            }

            string currentHash = await client.GetblockHashAsync(block.BlockIndex);
            if (currentHash == block.BlockHash)
            {
               return block;
            }

            log.LogDebug($"Rewinding block {block.BlockIndex}({block.BlockHash})");

            await storage.DeleteBlockAsync(block.BlockHash);
         }
      }

      public async Task<SyncBlockInfo> RewindToLastCompletedBlockAsync()
      {
         SyncBlockInfo lastBlock = storage.GetLatestBlock();

         if (lastBlock == null)
            return null;

         while (lastBlock != null && lastBlock.SyncComplete == false)
         {
            log.LogDebug($"Rewinding block {lastBlock.BlockIndex}({lastBlock.BlockHash})");

            await storage.DeleteBlockAsync(lastBlock.BlockHash);
            lastBlock = storage.BlockByIndex(lastBlock.BlockIndex - 1);
         }

         return lastBlock;
      }

      private SyncPoolTransactions FindPoolInternal(SyncConnection connection)
      {
         IBlockchainClient client = clientFactory.Create(connection);

         IEnumerable<string> memPool = client.GetRawMemPool();

         var currentMemoryPool = new HashSet<string>(memPool);
         var currentTable = new HashSet<string>(globalState.LocalMempoolView.Keys);

         var newTransactions = currentMemoryPool.Except(currentTable).ToList();
         var deleteTransaction = currentTable.Except(currentMemoryPool).ToList();

         // limit to 1k trx per loop
         newTransactions = newTransactions.Take(1000).ToList();

         // entries deleted from mempool on the node
         // we also delete it in our store
         if (deleteTransaction.Any())
         {
            List<string> toRemoveFromMempool = deleteTransaction;

            storage.DeleteTransactionsFromMempool(toRemoveFromMempool);

            foreach (string mempooltrx in toRemoveFromMempool)
               globalState.LocalMempoolView.Remove(mempooltrx, out _);
         }

         return new SyncPoolTransactions { Transactions = newTransactions };
      }

      private class tcalc
      {
         public string item;
         public DecodedRawTransaction result;
      }

      private SyncBlockTransactionsOperation SyncBlockTransactions(IBlockchainClient client, SyncConnection connection, IEnumerable<string> transactionsToSync, bool throwIfNotFound)
      {
         var itemList = transactionsToSync.Select(t => new tcalc { item = t }).ToList();

         var options = new ParallelOptions { MaxDegreeOfParallelism = configuration.ParallelRequestsToTransactionRpc };
         Parallel.ForEach(itemList, options, (item) =>
         {
            try
            {
               item.result = client.GetRawTransaction(item.item, 0);
            }
            catch (BitcoinClientException bce)
            {
               if (!throwIfNotFound && bce.IsTransactionNotFound())
               {
                  //// the transaction was not found in the client,
                  //// if this is a pool sync we assume the transaction was initially found in the pool and became invalid.
                  return;
               }

               throw;
            }
         });

         IEnumerable<Transaction> transactions = itemList.Where(t => t.result != null).Select(s =>
         {
            Transaction trx = connection.Network.Consensus.ConsensusFactory.CreateTransaction(s.result.Hex);
            trx.PrecomputeHash(false, true);
            return trx;
         });

         return new SyncBlockTransactionsOperation { Transactions = transactions.ToList() };
      }

      private SyncBlockTransactionsOperation SyncPoolInternal(SyncConnection connection, SyncPoolTransactions poolTransactions)
      {
         IBlockchainClient client = clientFactory.Create(connection);

         SyncBlockTransactionsOperation returnBlock = SyncBlockTransactions(client, connection, poolTransactions.Transactions, false);

         return returnBlock;
      }

      private SyncBlockTransactionsOperation SyncBlockInternal(SyncConnection connection, BlockInfo block)
      {
         var client = clientFactory.Create(connection);

         string hex = client.GetBlockHex(block.Hash);

         var blockItem = Consensus.BlockInfo.Block.Parse(hex, connection.Network.Consensus.ConsensusFactory);

         foreach (Transaction blockItemTransaction in blockItem.Transactions)
         {
            blockItemTransaction.PrecomputeHash(false, true);
         }

         SyncBlockTransactionsOperation returnBlock = transactionOperationBuilder.BuildFromClientData(block, blockItem);

         return returnBlock;
      }
   }
}
