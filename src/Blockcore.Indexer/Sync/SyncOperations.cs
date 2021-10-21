using Blockcore.Indexer.Crypto;

namespace Blockcore.Indexer.Sync
{
   using System.Collections.Generic;
   using System.Linq;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Client;
   using Blockcore.Indexer.Client.Types;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage;
   using Microsoft.Extensions.Caching.Memory;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;
   using NBitcoin;
   using Blockcore.Consensus.TransactionInfo;
   using Blockcore.Consensus.ScriptInfo;
   using Blockcore.Consensus.BlockInfo;

   /// <summary>
   /// The CoinOperations interface.
   /// </summary>
   public class SyncOperations : ISyncOperations
   {
      private readonly IStorage storage;

      private readonly ILogger<SyncOperations> log;

      private readonly IndexerSettings configuration;

      private readonly System.Diagnostics.Stopwatch watch;

      private readonly IMemoryCache cache;

      private readonly MemoryCacheEntryOptions cacheOptions;

      /// <summary>
      /// Initializes a new instance of the <see cref="SyncOperations"/> class.
      /// </summary>
      public SyncOperations(IStorage storage, ILogger<SyncOperations> logger, IOptions<IndexerSettings> configuration, IMemoryCache cache)
      {
         this.configuration = configuration.Value;
         log = logger;
         this.storage = storage;
         this.cache = cache;

         // Register the cold staking template.
         StandardScripts.RegisterStandardScriptTemplate(ColdStakingScriptTemplate.Instance);

         watch = Stopwatch.Start();
         cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheKeys.BlockCountTime);
      }

      public long GetBlockCount(BitcoinClient client)
      {
         if (!cache.TryGetValue(CacheKeys.BlockCount, out long cacheEntry))
         {
            cacheEntry = client.GetBlockCount();

            // Save data in cache.
            cache.Set(CacheKeys.BlockCount, cacheEntry, cacheOptions);
         }

         return cacheEntry;
      }

      public SyncPoolTransactions FindPoolTransactions(SyncConnection connection, SyncingBlocks container)
      {
         return FindPoolInternal(connection, container);
      }

      public SyncBlockTransactionsOperation SyncPool(SyncConnection connection, SyncPoolTransactions poolTransactions)
      {
         return SyncPoolInternal(connection, poolTransactions);
      }

      public SyncBlockTransactionsOperation FetchFullBlock(SyncConnection connection, BlockInfo block)
      {
         return SyncBlockInternal(connection, block);
      }

      public async Task<Storage.Types.SyncBlockInfo> RewindToBestChain(SyncConnection connection)
      {
         BitcoinClient client = CryptoClientFactory.Create(connection);

         while (true)
         {
            Storage.Types.SyncBlockInfo block = storage.GetLatestBlock();

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

            storage.DeleteBlock(block.BlockHash);
         }
      }

      public Storage.Types.SyncBlockInfo RewindToLastCompletedBlock()
      {
         Storage.Types.SyncBlockInfo lastBlock = storage.GetLatestBlock();

         if (lastBlock == null)
            return null;

         while (lastBlock != null && lastBlock.SyncComplete == false)
         {
            log.LogDebug($"Rewinding block {lastBlock.BlockIndex}({lastBlock.BlockHash})");

            storage.DeleteBlock(lastBlock.BlockHash);
            lastBlock = storage.BlockByIndex(lastBlock.BlockIndex - 1);
         }

         return lastBlock;
      }

      private SyncPoolTransactions FindPoolInternal(SyncConnection connection, SyncingBlocks syncingBlocks)
      {
         BitcoinClient client = CryptoClientFactory.Create(connection);

         IEnumerable<string> memPool = client.GetRawMemPool();

         var currentMemoryPool = new HashSet<string>(memPool);
         var currentTable = new HashSet<string>(syncingBlocks.LocalMempoolView);

         var newTransactions = currentMemoryPool.Except(currentTable).ToList();
         var deleteTransaction = currentTable.Except(currentMemoryPool).ToList();

         syncingBlocks.LocalMempoolView.AddRange(newTransactions);
         deleteTransaction.ForEach(t => syncingBlocks.LocalMempoolView.Remove(t));

         return new SyncPoolTransactions { Transactions = newTransactions };
      }

      private class tcalc
      {
         public string item;
         public DecodedRawTransaction result;
      }

      private SyncBlockTransactionsOperation SyncBlockTransactions(BitcoinClient client, SyncConnection connection, IEnumerable<string> transactionsToSync, bool throwIfNotFound)
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

         IEnumerable<Transaction> transactions = itemList.Select(s =>
         {
            Transaction trx = connection.Network.Consensus.ConsensusFactory.CreateTransaction(s.result.Hex);
            trx.PrecomputeHash(false, true);
            return trx;
         });

         return new SyncBlockTransactionsOperation { Transactions = transactions.ToList() };
      }

      private SyncBlockTransactionsOperation SyncPoolInternal(SyncConnection connection, SyncPoolTransactions poolTransactions)
      {
         BitcoinClient client = CryptoClientFactory.Create(connection);

         SyncBlockTransactionsOperation returnBlock = SyncBlockTransactions(client, connection, poolTransactions.Transactions, false);

         return returnBlock;
      }

      private SyncBlockTransactionsOperation SyncBlockInternal(SyncConnection connection, BlockInfo block)
      {
         BitcoinClient client = CryptoClientFactory.Create(connection);

         string hex = client.GetBlockHex(block.Hash);

         var blockItem = Block.Parse(hex, connection.Network.Consensus.ConsensusFactory);

         foreach (Transaction blockItemTransaction in blockItem.Transactions)
         {
            blockItemTransaction.PrecomputeHash(false, true);
         }

         var returnBlock = new SyncBlockTransactionsOperation { BlockInfo = block, Transactions = blockItem.Transactions };

         return returnBlock;
      }
   }
}
