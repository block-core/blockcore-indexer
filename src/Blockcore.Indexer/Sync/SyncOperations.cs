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

      public SyncBlockOperation FindBlock(SyncConnection connection, SyncingBlocks container)
      {
         return FindBlockInternal(connection, container);
      }

      public SyncPoolTransactions FindPoolTransactions(SyncConnection connection, SyncingBlocks container)
      {
         return FindPoolInternal(connection, container);
      }

      public SyncBlockTransactionsOperation SyncPool(SyncConnection connection, SyncPoolTransactions poolTransactions)
      {
         return SyncPoolInternal(connection, poolTransactions);
      }

      public SyncBlockTransactionsOperation SyncBlock(SyncConnection connection, BlockInfo block)
      {
         return SyncBlockInternal(connection, block);
      }

      public async Task CheckBlockReorganization(SyncConnection connection)
      {
         while (true)
         {
            Storage.Types.SyncBlockInfo block = storage.BlockGetBlockCount(1).FirstOrDefault();

            if (block == null)
            {
               break;
            }

            BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);
            string currentHash = await client.GetblockHashAsync(block.BlockIndex);
            if (currentHash == block.BlockHash)
            {
               break;
            }

            log.LogInformation($"SyncOperations: Deleting block {block.BlockIndex}");

            storage.DeleteBlock(block.BlockHash);
         }
      }

      private SyncBlockOperation GetNextBlockToSync(BitcoinClient client, SyncConnection connection, long lastCryptoBlockIndex, SyncingBlocks syncingBlocks)
      {
         if (syncingBlocks.LastBlock == null)
         {
            // because inserting blocks is sequential we'll use the indexed 'height' filed to check if the last block is incomplete.
            var incomplete = storage.BlockGetBlockCount(6).Where(b => !b.SyncComplete).ToList(); ////this.storage.BlockGetIncompleteBlocks().ToList();

            Storage.Types.SyncBlockInfo incompleteToSync = incomplete.OrderBy(o => o.BlockIndex).FirstOrDefault(f => !syncingBlocks.CurrentSyncing.ContainsKey(f.BlockHash));

            if (incompleteToSync != null)
            {
               BlockInfo incompleteBlock = client.GetBlock(incompleteToSync.BlockHash);

               return new SyncBlockOperation { BlockInfo = incompleteBlock, IncompleteBlock = true, LastCryptoBlockIndex = lastCryptoBlockIndex };
            }

            string blockHashsToSync;

            var blocks = storage.BlockGetBlockCount(1).ToList();

            if (blocks.Any())
            {
               long lastBlockIndex = blocks.First().BlockIndex;

               if (lastBlockIndex == lastCryptoBlockIndex)
               {
                  // No new blocks.
                  return default(SyncBlockOperation);
               }

               blockHashsToSync = client.GetblockHash(lastBlockIndex + 1);
            }
            else
            {
               // No blocks in store start from zero configured block index.
               blockHashsToSync = client.GetblockHash(connection.StartBlockIndex);
            }

            BlockInfo nextNewBlock = client.GetBlock(blockHashsToSync);

            syncingBlocks.LastBlock = nextNewBlock;

            return new SyncBlockOperation { BlockInfo = nextNewBlock, LastCryptoBlockIndex = lastCryptoBlockIndex };
         }

         if (syncingBlocks.LastBlock.Height == lastCryptoBlockIndex)
         {
            // No new blocks.
            return default(SyncBlockOperation);
         }

         string nextHash = client.GetblockHash(syncingBlocks.LastBlock.Height + 1);

         BlockInfo nextBlock = client.GetBlock(nextHash);

         syncingBlocks.LastBlock = nextBlock;

         return new SyncBlockOperation { BlockInfo = nextBlock, LastCryptoBlockIndex = lastCryptoBlockIndex };
      }

      private SyncBlockOperation FindBlockInternal(SyncConnection connection, SyncingBlocks syncingBlocks)
      {
         watch.Restart();

         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         syncingBlocks.LastClientBlockIndex = GetBlockCount(client);

         SyncBlockOperation blockToSync = GetNextBlockToSync(client, connection, syncingBlocks.LastClientBlockIndex, syncingBlocks);

         if (blockToSync != null && blockToSync.BlockInfo != null)
         {
            syncingBlocks.CurrentSyncing.TryAdd(blockToSync.BlockInfo.Hash, blockToSync.BlockInfo);
         }

         watch.Stop();

         return blockToSync;
      }

      private SyncPoolTransactions FindPoolInternal(SyncConnection connection, SyncingBlocks syncingBlocks)
      {
         watch.Restart();

         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         IEnumerable<string> memPool = client.GetRawMemPool();

         var currentMemoryPool = new HashSet<string>(memPool);
         var currentTable = new HashSet<string>(syncingBlocks.CurrentPoolSyncing);

         var newTransactions = currentMemoryPool.Except(currentTable).ToList();
         var deleteTransaction = currentTable.Except(currentMemoryPool).ToList();

         //var newTransactionsLimited = newTransactions.Count() < 1000 ? newTransactions : newTransactions.Take(1000).ToList();

         syncingBlocks.CurrentPoolSyncing.AddRange(newTransactions);
         deleteTransaction.ForEach(t => syncingBlocks.CurrentPoolSyncing.Remove(t));

         watch.Stop();

         log.LogDebug($"SyncPool: Seconds = {watch.Elapsed.TotalSeconds} - New Transactions = {newTransactions.Count()}");

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
         watch.Restart();

         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         SyncBlockTransactionsOperation returnBlock = SyncBlockTransactions(client, connection, poolTransactions.Transactions, false);

         watch.Stop();

         int transactionCount = returnBlock.Transactions.Count();
         double totalSeconds = watch.Elapsed.TotalSeconds;

         log.LogDebug($"SyncPool: Seconds = {watch.Elapsed.TotalSeconds} - Transactions = {transactionCount}");

         return returnBlock;
      }

      private SyncBlockTransactionsOperation SyncBlockInternal(SyncConnection connection, BlockInfo block)
      {
         System.Diagnostics.Stopwatch watch = Stopwatch.Start();

         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);

         string hex = client.GetBlockHex(block.Hash);

         var blockItem = Block.Parse(hex, connection.Network.Consensus.ConsensusFactory);

         foreach (Transaction blockItemTransaction in blockItem.Transactions)
         {
            blockItemTransaction.PrecomputeHash(false, true);
         }

         //var blockItem = Block.Load(Encoders.Hex.DecodeData(hex), consensusFactory);
         var returnBlock = new SyncBlockTransactionsOperation { BlockInfo = block, Transactions = blockItem.Transactions };  //this.SyncBlockTransactions(client, connection, block.Transactions, true);

         watch.Stop();

         return returnBlock;
      }
   }
}
