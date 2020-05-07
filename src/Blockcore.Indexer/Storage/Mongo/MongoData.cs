using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Api.Handlers.Types;
using Blockcore.Indexer.Client;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Crypto;
using Blockcore.Indexer.Extensions;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Indexer.Storage.Mongo
{
   public enum TransactionUsedFilter
   {
      All = 0,
      Spent = 1,
      Unspent = 2
   }

   public class MongoData : IStorage
   {
      private readonly ILogger<MongoStorageOperations> log;

      private readonly MongoClient mongoClient;

      private readonly IMongoDatabase mongoDatabase;

      private readonly SyncConnection syncConnection;

      private readonly IndexerSettings configuration;

      private readonly ChainSettings chainConfiguration;

      private readonly System.Diagnostics.Stopwatch watch;

      public MongoData(ILogger<MongoStorageOperations> logger, SyncConnection connection, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainConfiguration)
      {
         configuration = nakoConfiguration.Value;
         this.chainConfiguration = chainConfiguration.Value;

         syncConnection = connection;
         log = logger;
         mongoClient = new MongoClient(configuration.ConnectionString.Replace("{Symbol}", this.chainConfiguration.Symbol.ToLower()));

         string dbName = configuration.DatabaseNameSubfix ? "Blockchain" + this.chainConfiguration.Symbol : "Blockchain";

         mongoDatabase = mongoClient.GetDatabase(dbName);
         MemoryTransactions = new ConcurrentDictionary<string, NBitcoin.Transaction>();

         // Make sure we only create a single instance of the watcher.
         watch = Stopwatch.Start();
      }

      public IMongoCollection<MapTransactionAddress> MapTransactionAddress
      {
         get
         {
            return mongoDatabase.GetCollection<MapTransactionAddress>("MapTransactionAddress");
         }
      }

      public IMongoCollection<MapTransactionBlock> MapTransactionBlock
      {
         get
         {
            return mongoDatabase.GetCollection<MapTransactionBlock>("MapTransactionBlock");
         }
      }

      public IMongoCollection<MapTransaction> MapTransaction
      {
         get
         {
            return mongoDatabase.GetCollection<MapTransaction>("MapTransaction");
         }
      }

      public IMongoCollection<MapBlock> MapBlock
      {
         get
         {
            return mongoDatabase.GetCollection<MapBlock>("MapBlock");
         }
      }

      public IMongoCollection<MapRichlist> MapRichlist
      {
         get
         {
            return mongoDatabase.GetCollection<MapRichlist>("MapRichlist");
         }
      }
      public IMongoCollection<PeerInfo> Peer
      {
         get
         {
            return mongoDatabase.GetCollection<PeerInfo>("Peer");
         }
      }

      public ConcurrentDictionary<string, NBitcoin.Transaction> MemoryTransactions { get; set; }

      public IEnumerable<SyncBlockInfo> BlockGetIncompleteBlocks()
      {
         // note this field is not indexed
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(info => info.SyncComplete, false);

         return MapBlock.Find(filter).ToList().Select(Convert);
      }

      /// <summary>
      /// This returns any number of blocks specified. Don't make this accessible to outside to avoid large queries.
      /// </summary>
      /// <param name="count"></param>
      /// <returns></returns>
      public IEnumerable<SyncBlockInfo> BlockGetBlockCount(int count)
      {
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Exists(info => info.BlockIndex);
         SortDefinition<MapBlock> sort = Builders<MapBlock>.Sort.Descending(info => info.BlockIndex);

         return MapBlock.Find(filter).Sort(sort).Limit(count).ToList().Select(Convert);
      }

      public IEnumerable<SyncBlockInfo> BlockGetCompleteBlockCount(int count)
      {
         var blocks = BlockGetBlockCount(2).ToList();
         return blocks.Where(b => b.SyncComplete);
      }

      public QueryTransaction GetTransaction(string transactionId)
      {
         Storage.Types.SyncTransactionInfo transaction = BlockTransactionGet(transactionId);
         Storage.Types.SyncTransactionItems transactionItems = TransactionItemsGet(transactionId);

         if (transactionItems == null)
         {
            return null;
         }

         var result = new QueryTransaction
         {
            Symbol = chainConfiguration.Symbol,
            BlockHash = transaction?.BlockHash ?? null,
            BlockIndex = transaction?.BlockIndex ?? null,
            Confirmations = transaction?.Confirmations ?? 0,
            Timestamp = transaction?.Timestamp ?? 0,
            TransactionId = transaction?.TransactionHash ?? transactionId,

            RBF = transactionItems.RBF,
            LockTime = transactionItems.LockTime.ToString(),
            Version = transactionItems.Version,
            IsCoinbase = transactionItems.IsCoinbase,
            IsCoinstake = transactionItems.IsCoinstake,

            Inputs = transactionItems.Inputs.Select(i => new QueryTransactionInput
            {
               CoinBase = i.InputCoinBase,
               InputAddress = i.InputAddress,
               InputIndex = i.PreviousIndex,
               InputTransactionId = i.PreviousTransactionHash,
               ScriptSig = i.ScriptSig,
               ScriptSigAsm = new Script(NBitcoin.DataEncoders.Encoders.Hex.DecodeData(i.ScriptSig)).ToString(),
               WitScript = i.WitScript,
               SequenceLock = i.SequenceLock
            }),
            Outputs = transactionItems.Outputs.Select(o => new QueryTransactionOutput
            {
               Address = o.Address,
               Balance = o.Value,
               Index = o.Index,
               OutputType = o.OutputType,
               ScriptPubKey = o.ScriptPubKey,
               SpentInTransaction = o.SpentInTransaction,
               ScriptPubKeyAsm = new Script(NBitcoin.DataEncoders.Encoders.Hex.DecodeData(o.ScriptPubKey)).ToString()
            }),
         };

         return result;
      }

      /// <summary>
      /// Returns block information in the section specified with offset and limit. If offset is set to 0, then the last page is returned.
      /// </summary>
      /// <param name="offset">Set to zero if last page should be returned.</param>
      /// <param name="limit">Amount of items to return.</param>
      /// <returns></returns>
      public QueryResult<SyncBlockInfo> Blocks(int offset, int limit)
      {
         FilterDefinitionBuilder<MapBlock> filterBuilder = Builders<MapBlock>.Filter;
         FilterDefinition<MapBlock> filter = filterBuilder.Empty;

         // Skip and Limit only supports int, so we can't support long amount of documents.
         int total = (int)MapBlock.Find(filter).CountDocuments();

         // If the offset is not set, or set to 0 implicit, we'll reverse the query and grab last page as oppose to first.
         if (offset == 0)
         {
            offset = (total - limit) + 1; // +1 to counteract the Skip -1 below.
         }

         IEnumerable<SyncBlockInfo> list = MapBlock.Find(filter)
                   .SortBy(p => p.BlockIndex)
                   .Skip(offset - 1) // 1 based index, so we'll subtract one.
                   .Limit(limit)
                   .ToList().Select(Convert);

         return new QueryResult<SyncBlockInfo> { Items = list, Total = total, Offset = offset, Limit = limit };
      }

      public SyncBlockInfo BlockByIndex(long blockIndex)
      {
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(info => info.BlockIndex, blockIndex);

         return MapBlock.Find(filter).ToList().Select(Convert).FirstOrDefault();
      }

      public SyncBlockInfo BlockByHash(string blockHash)
      {
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(info => info.BlockHash, blockHash);

         return MapBlock.Find(filter).ToList().Select(Convert).FirstOrDefault();
      }

      public void InsertBlock(MapBlock info)
      {
         MapBlock.InsertOne(info);
      }

      /// <summary>
      /// Inserts or updates a peer info instance. Returns the number of modified entries.
      /// </summary>
      /// <param name="info"></param>
      /// <returns></returns>
      public async Task<long> InsertPeer(PeerInfo info)
      {
         // Always update the LastSeen.
         info.LastSeen = DateTime.UtcNow;

         ReplaceOneResult replaceOneResult = await Peer.ReplaceOneAsync(doc => doc.Addr == info.Addr, info, new ReplaceOptions { IsUpsert = true });

         return replaceOneResult.ModifiedCount;
      }

      public List<PeerInfo> GetPeerFromDate(DateTime date)
      {
         FilterDefinition<PeerInfo> filter = Builders<PeerInfo>.Filter.Gt(addr => addr.LastSeen, date);
         return Peer.Find(filter).ToList();
      }

      public SyncRawTransaction TransactionGetByHash(string trxHash)
      {
         FilterDefinition<MapTransaction> filter = Builders<MapTransaction>.Filter.Eq(info => info.TransactionId, trxHash);

         return MapTransaction.Find(filter).ToList().Select(t => new SyncRawTransaction { TransactionHash = trxHash, RawTransaction = t.RawTransaction }).FirstOrDefault();
      }

      public void InsertTransaction(MapTransaction info)
      {
         MapTransaction.InsertOne(info);
      }

      public void CompleteBlock(string blockHash)
      {
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(blockInfo => blockInfo.BlockHash, blockHash);
         UpdateDefinition<MapBlock> update = Builders<MapBlock>.Update.Set(blockInfo => blockInfo.SyncComplete, true);
         MapBlock.UpdateOne(filter, update);
      }

      public void MarkOutput(string transaction, int index, string spendingTransactionId, long spendingBlockIndex)
      {
         FilterDefinition<MapTransactionAddress> filter = Builders<MapTransactionAddress>.Filter.Eq(addr => addr.Id, string.Format("{0}-{1}", transaction, index));
         UpdateDefinition<MapTransactionAddress> update = Builders<MapTransactionAddress>.Update
             .Set(blockInfo => blockInfo.SpendingTransactionId, spendingTransactionId)
             .Set(blockInfo => blockInfo.SpendingBlockIndex, spendingBlockIndex);

         MapTransactionAddress.UpdateOne(filter, update);
      }

      public string GetSpendingTransaction(string transaction, int index)
      {
         FilterDefinition<MapTransactionAddress> filter = Builders<MapTransactionAddress>.Filter.Eq(addr => addr.Id, string.Format("{0}-{1}", transaction, index));

         return MapTransactionAddress.Find(filter).ToList().Select(t => t.SpendingTransactionId).FirstOrDefault();
      }

      public SyncTransactionInfo BlockTransactionGet(string transactionId)
      {
         FilterDefinition<MapTransactionBlock> filter = Builders<MapTransactionBlock>.Filter.Eq(info => info.TransactionId, transactionId);

         MapTransactionBlock trx = MapTransactionBlock.Find(filter).FirstOrDefault();
         if (trx == null)
         {
            return null;
         }

         SyncBlockInfo current = GetLatestBlock();

         SyncBlockInfo blk = BlockByIndex(trx.BlockIndex);

         return new SyncTransactionInfo
         {
            BlockIndex = trx.BlockIndex,
            BlockHash = blk.BlockHash,
            Timestamp = blk.BlockTime,
            TransactionHash = trx.TransactionId,
            Confirmations = current.BlockIndex - trx.BlockIndex
         };
      }

      public SyncTransactionItems TransactionItemsGet(string transactionId)
      {
         NBitcoin.Transaction transaction;

         // Try to find the trx in disk
         SyncRawTransaction rawtrx = TransactionGetByHash(transactionId);

         if (rawtrx == null)
         {
            BitcoinClient client = CryptoClientFactory.Create(syncConnection.ServerDomain, syncConnection.RpcAccessPort, syncConnection.User, syncConnection.Password, syncConnection.Secure);

            Client.Types.DecodedRawTransaction res = client.GetRawTransactionAsync(transactionId, 0).Result;

            if (res.Hex == null)
            {
               return null;
            }

            transaction = syncConnection.Network.Consensus.ConsensusFactory.CreateTransaction(res.Hex);
            transaction.PrecomputeHash(false, true);
         }
         else
         {
            transaction = syncConnection.Network.Consensus.ConsensusFactory.CreateTransaction(rawtrx.RawTransaction);
            transaction.PrecomputeHash(false, true);
         }

         var ret = new SyncTransactionItems
         {
            RBF = transaction.RBF,
            LockTime = transaction.LockTime.ToString(),
            Version = transaction.Version,
            IsCoinbase = transaction.IsCoinBase,
            IsCoinstake = syncConnection.Network.Consensus.IsProofOfStake && transaction.IsCoinStake,
            Inputs = transaction.Inputs.Select(v => new SyncTransactionItemInput
            {
               PreviousTransactionHash = v.PrevOut.Hash.ToString(),
               PreviousIndex = (int)v.PrevOut.N,
               WitScript = v.WitScript.ToScript().ToHex(),
               ScriptSig = v.ScriptSig.ToHex(),
               InputAddress = ScriptToAddressParser.GetSignerAddress(syncConnection.Network, v.ScriptSig),
               SequenceLock = v.Sequence.ToString(),
            }).ToList(),
            Outputs = transaction.Outputs.Select((output, index) => new SyncTransactionItemOutput
            {
               Address = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey)?.FirstOrDefault(),
               Index = index,
               Value = output.Value,
               OutputType = StandardScripts.GetTemplateFromScriptPubKey(output.ScriptPubKey)?.Type.ToString(),
               ScriptPubKey = output.ScriptPubKey.ToHex()
            }).ToList()
         };

         // try to fetch spent outputs
         foreach (SyncTransactionItemOutput output in ret.Outputs)
         {
            output.SpentInTransaction = GetSpendingTransaction(transactionId, output.Index);
         }

         return ret;
      }

      public QueryResult<MapRichlist> Richlist(int offset, int limit)
      {
         FilterDefinitionBuilder<MapRichlist> filterBuilder = Builders<MapRichlist>.Filter;
         FilterDefinition<MapRichlist> filter = filterBuilder.Empty;

         // Skip and Limit only supports int, so we can't support long amount of documents.
         int total = (int)MapRichlist.Find(filter).CountDocuments();

         // If the offset is not set, or set to 0 implicit, we'll reverse the query and grab last page as oppose to first.
         if (offset == 0)
         {
            offset = (total - limit) + 1; // +1 to counteract the Skip -1 below.
         }

         IEnumerable<MapRichlist> list = MapRichlist.Find(filter)
                   .SortBy(p => p.Balance)
                   .Skip(offset - 1) // 1 based index, so we'll subtract one.
                   .Limit(limit)
                   .ToList();

         return new QueryResult<MapRichlist> { Items = list, Total = total, Offset = offset, Limit = limit };
      }
      /// <summary>
      /// Get transactions that belongs to a block.
      /// </summary>
      /// <param name="hash"></param>
      public QueryResult<SyncTransactionInfo> TransactionsByBlock(string hash, int offset, int limit)
      {
         SyncBlockInfo blk = BlockByHash(hash);
         return TransactionsByBlock(blk.BlockIndex, offset, limit);
      }

      /// <summary>
      /// Get transactions that belongs to a block.
      /// </summary>
      /// <param name="index"></param>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      public QueryResult<SyncTransactionInfo> TransactionsByBlock(long index, int offset, int limit)
      {
         FilterDefinition<MapTransactionBlock> filter = Builders<MapTransactionBlock>.Filter.Eq(info => info.BlockIndex, index);

         int total = (int)MapTransactionBlock.Find(filter).CountDocuments();

         // Can we do sorting?
         // SortDefinition<MapBlock> sort = Builders<MapBlock>.Sort.Descending(info => info.BlockIndex);

         IEnumerable<SyncTransactionInfo> list = MapTransactionBlock.Find(filter)
                   // .SortBy(p => p.BlockIndex) // Can we do sort?
                   .Skip(offset)
                   .Limit(limit)
                   .ToList().Select(s => new SyncTransactionInfo
                   {
                      TransactionHash = s.TransactionId,
                   });

         return new QueryResult<SyncTransactionInfo>
         {
            Items = list,
            Offset = offset,
            Limit = limit,
            Total = total
         };
      }

      public SyncBlockInfo GetLatestBlock()
      {
         SyncBlockInfo current = BlockGetBlockCount(1).FirstOrDefault();
         return current;
      }


      public QueryResult<QueryTransaction> AddressTransactions(string address, long confirmations, bool unconfirmed, TransactionUsedFilter used, int offset, int limit)
      {
         // Create a query against transactions on the specified address.
         IQueryable<MapTransactionAddress> filter = AddressTransactionFilter(address);

         if (confirmations > 0)
         {
            SyncBlockInfo current = GetLatestBlock();

            // Calculate the minimum height to get confirmations required.
            long height = current.BlockIndex - confirmations;

            if (unconfirmed)
            {
               // Check if BlockIndex is higher than the height. Height is (Tip - Confirmations).
               filter = filter.Where(s => s.BlockIndex > height);
            }
            else
            {
               // Check if BlockIndex is lower or equal to height. Height is (Tip - Confirmations).
               filter = filter.Where(s => s.BlockIndex <= height);
            }
         }

         // Filter on spent, unspent or just include all (default).
         if (used == TransactionUsedFilter.Spent)
         {
            filter = filter.Where(s => s.SpendingTransactionId != null);
         }
         else if (used == TransactionUsedFilter.Unspent)
         {
            filter = filter.Where(s => s.SpendingTransactionId == null);
         }

         filter = filter.OrderByDescending(s => s.BlockIndex);

         // This will first perform one db query.
         int total = filter.Count();

         // This will perform a query and return only transaction ID of the filtered results.
         var list = filter.Skip(offset).Take(limit).Select(t => t.TransactionId).ToList();

         // Loop all transaction IDs and get the transaction object.
         IEnumerable<QueryTransaction> transactions = list.Select(id => GetTransaction(id));

         return new QueryResult<QueryTransaction>
         {
            Items = transactions,
            Offset = offset,
            Limit = limit,
            Total = total
         };
      }

      /// <summary>
      /// Calculates the balance for specified address. When confirmations is 0 (default), then all transactions (excluding mempool) will be counted.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <param name="includeMempool"></param>
      /// <returns></returns>
      public AddressBalance AddressBalance(string address, long confirmations = 0, bool includeMempool = false)
      {
         // Create a query against transactions on the specified address.
         IQueryable<MapTransactionAddress> filter = AddressTransactionFilter(address);

         long confirmed = 0;
         long unconfirmed = 0;

         // TODO: Continue work on the includeMempool when other stuff are finished.
         //if (includeMempool)
         //{
         //   // this creates a copy of the collection (to avoid thread issues)
         //   ICollection<Transaction> pool = MemoryTransactions.Values;

         //   if (pool.Any())
         //   {
         //      // mark trx in output as spent if they exist in the pool
         //      // List<MapTransactionAddress> addrsupdate = addrs;

         //      GetPoolOutputs(pool).ForEach(f =>
         //      {
         //         string outputAddress = f.Item1.PrevOut.Hash.ToString();

         //         // TODO: Verify why Index mattered in the query here... we don't have that available now that we have flipped how we look into the mempool.
         //         //MapTransactionAddress adr = addrsupdate.FirstOrDefault(a => a.TransactionId == f.Item1.PrevOut.Hash.ToString() && a.Index == f.Item1.PrevOut.N);

         //         if (adr != null)
         //         {
         //            adr.SpendingTransactionId = f.Item2;
         //         }
         //      });

         //      // if only spendable transactions are to be returned we need to remove 
         //      // any that have been marked as spent by a transaction in the pool
         //      if (availableOnly)
         //      {
         //         addrs = addrs.Where(d => d.SpendingTransactionId == null).ToList();
         //      }

         //      // add all pool transactions to main output
         //      var paddr = PoolToMapTransactionAddress(pool, address).ToList();
         //      addrs = addrs.OrderByDescending(s => s.BlockIndex).Concat(paddr).ToList();
         //   }
         //}

         // Make sure we only compute extra queries when we absolutely need to.
         if (confirmations > 0)
         {
            SyncBlockInfo current = GetLatestBlock();

            // Calculate the minimum height to get confirmations required.
            long height = current.BlockIndex - confirmations;

            // Check if BlockIndex is lower or equal to height. Height is (Tip - Confirmations).
            confirmed = filter.Where(s => s.BlockIndex <= height).Sum(s => s.Value);

            // Check if BlockIndex is higher than the height. Height is (Tip - Confirmations).
            unconfirmed = filter.Where(s => s.BlockIndex > height).Sum(s => s.Value);
         }
         else
         {
            // Check if BlockIndex is lower or equal to height. Height is (Tip - Confirmations).
            confirmed = filter.Sum(s => s.Value);
         }

         long sent = filter.Where(s => s.SpendingTransactionId != null).Sum(s => s.Value);
         long available = confirmed - sent;

         var balance = new AddressBalance
         {
            Address = address,
            Available = available,
            Received = confirmed,
            Sent = sent,
            Unconfirmed = unconfirmed
         };

         return balance;
      }

      public void DeleteBlock(string blockHash)
      {
         SyncBlockInfo block = BlockByHash(blockHash);

         // delete the outputs
         FilterDefinition<MapTransactionAddress> addrFilter = Builders<MapTransactionAddress>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         MapTransactionAddress.DeleteMany(addrFilter);

         // delete the transaction
         FilterDefinition<MapTransactionBlock> transactionFilter = Builders<MapTransactionBlock>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         MapTransactionBlock.DeleteMany(transactionFilter);

         // delete the block itself.
         FilterDefinition<MapBlock> blockFilter = Builders<MapBlock>.Filter.Eq(info => info.BlockHash, blockHash);
         MapBlock.DeleteOne(blockFilter);
      }

      public QueryResult<NBitcoin.Transaction> GetMemoryTransactions(int offset, int limit)
      {
         ICollection<Transaction> list = MemoryTransactions.Values;

         var queryResult = new QueryResult<NBitcoin.Transaction>
         {
            Items = list.Skip(offset - 1).Take(limit), // 1 based index, so we'll subtract one.
            Total = list.Count,
            Offset = offset,
            Limit = limit
         };

         return queryResult;
      }

      public int GetMemoryTransactionsCount()
      {
         return MemoryTransactions.Values.Count;
      }

      private SyncBlockInfo Convert(MapBlock block)
      {
         return new SyncBlockInfo
         {
            BlockIndex = block.BlockIndex,
            BlockSize = block.BlockSize,
            BlockHash = block.BlockHash,
            BlockTime = block.BlockTime,
            NextBlockHash = block.NextBlockHash,
            PreviousBlockHash = block.PreviousBlockHash,
            TransactionCount = block.TransactionCount,
            Nonce = block.Nonce,
            ChainWork = block.ChainWork,
            Difficulty = block.Difficulty,
            Merkleroot = block.Merkleroot,
            PosModifierv2 = block.PosModifierv2,
            PosHashProof = block.PosHashProof,
            PosFlags = block.PosFlags,
            PosChainTrust = block.PosChainTrust,
            PosBlockTrust = block.PosBlockTrust,
            PosBlockSignature = block.PosBlockSignature,
            Confirmations = block.Confirmations,
            Bits = block.Bits,
            Version = block.Version,
            SyncComplete = block.SyncComplete
         };
      }

      private IQueryable<MapTransactionAddress> AddressTransactionFilter(string address)
      {
         FilterDefinitionBuilder<MapTransactionAddress> builder = Builders<MapTransactionAddress>.Filter;

         IQueryable<MapTransactionAddress> filter = MapTransactionAddress.AsQueryable().Where(t => t.Addresses.Contains(address));

         // TODO: Add again in the future.
         //if (availableOnly)
         //{
         //   // we only want spendable transactions    
         //   // filter = filter & builder.Eq(info => info.SpendingTransactionId, null);
         //   filter = filter.Where(info => info.SpendingTransactionId == null);
         //}

         filter = filter.OrderByDescending(t => t.BlockIndex);

         return filter;
      }

      private AddressBalance GetTransactionsByAddress(long confirmations, long blockIndex, string address)
      {
         // Create a query against transactions on the specified address.
         IQueryable<MapTransactionAddress> filter = AddressTransactionFilter(address);

         // Calculate the minimum height to get confirmations required.
         long height = blockIndex - confirmations;

         // Check if BlockIndex is lower or equal to height. Height is (Tip - Confirmations).
         long confirmed = filter.Where(s => s.BlockIndex <= height).Sum(s => s.Value);

         // Check if BlockIndex is higher than the height. Height is (Tip - Confirmations).
         long unconfirmed = filter.Where(s => s.BlockIndex > height).Sum(s => s.Value);

         long sent = filter.Where(s => s.SpendingTransactionId != null).Sum(s => s.Value);
         long available = confirmed - sent;

         var balance = new AddressBalance
         {
            Address = address,
            Available = available,
            Received = confirmed,
            Sent = sent,
            Unconfirmed = unconfirmed
         };

         return balance;
      }

      private IEnumerable<SyncTransactionAddressItem> SelectAddressWithPool(SyncBlockInfo current, string address, bool availableOnly)
      {
         FilterDefinitionBuilder<MapTransactionAddress> builder = Builders<MapTransactionAddress>.Filter;
         var addressFiler = new List<string> { address };
         FilterDefinition<MapTransactionAddress> filter = builder.AnyIn(transactionAddress => transactionAddress.Addresses, addressFiler);

         if (availableOnly)
         {
            // we only want spendable transactions    
            filter = filter & builder.Eq(info => info.SpendingTransactionId, null);
         }

         watch.Restart();

         SortDefinition<MapTransactionAddress> sort = Builders<MapTransactionAddress>.Sort.Descending(info => info.BlockIndex);

         var addrs = MapTransactionAddress.Find(filter).Sort(sort).ToList();

         watch.Stop();

        // log.LogInformation($"Select: Seconds = {watch.Elapsed.TotalSeconds} - UnspentOnly = {availableOnly} - Addr = {address} - Items = {addrs.Count()}");

         // this creates a copy of the collection (to avoid thread issues)
         ICollection<Transaction> pool = MemoryTransactions.Values;

         if (pool.Any())
         {
            // mark trx in output as spent if they exist in the pool
            List<MapTransactionAddress> addrsupdate = addrs;
            GetPoolOutputs(pool).ForEach(f =>
            {
               MapTransactionAddress adr = addrsupdate.FirstOrDefault(a => a.TransactionId == f.Item1.PrevOut.Hash.ToString() && a.Index == f.Item1.PrevOut.N);
               if (adr != null)
               {
                  adr.SpendingTransactionId = f.Item2;
               }
            });

            // if only spendable transactions are to be returned we need to remove 
            // any that have been marked as spent by a transaction in the pool
            if (availableOnly)
            {
               addrs = addrs.Where(d => d.SpendingTransactionId == null).ToList();
            }

            // add all pool transactions to main output
            var paddr = PoolToMapTransactionAddress(pool, address).ToList();
            addrs = addrs.OrderByDescending(s => s.BlockIndex).Concat(paddr).ToList();
         }

         // map to return type and calculate confirmations
         return addrs.Select(s => new SyncTransactionAddressItem
         {
            Address = address,
            Index = s.Index,
            TransactionHash = s.TransactionId,
            BlockIndex = s.BlockIndex == -1 ? default(long?) : s.BlockIndex,
            Value = s.Value,
            Confirmations = s.BlockIndex == -1 ? 0 : current.BlockIndex - s.BlockIndex + 1,
            SpendingTransactionHash = s.SpendingTransactionId,
            SpendingBlockIndex = s.SpendingBlockIndex,
            CoinBase = s.CoinBase,
            CoinStake = s.CoinStake,
            ScriptHex = new Script(Encoders.Hex.DecodeData(s.ScriptHex)).ToString(),
            Type = StandardScripts.GetTemplateFromScriptPubKey(new Script(Encoders.Hex.DecodeData(s.ScriptHex)))?.Type.ToString(),
            Time = s.BlockIndex == -1 ? UnixUtils.DateToUnixTimestamp(DateTime.UtcNow) : BlockByIndex(s.BlockIndex).BlockTime
         });
      }

      private IEnumerable<Tuple<NBitcoin.TxIn, string>> GetPoolOutputs(IEnumerable<NBitcoin.Transaction> pool)
      {
         return pool.SelectMany(s => s.Inputs.Select(v => new Tuple<NBitcoin.TxIn, string>(v, s.GetHash().ToString())));
      }

      private IEnumerable<MapTransactionAddress> PoolToMapTransactionAddress(IEnumerable<NBitcoin.Transaction> pool, string address)
      {
         foreach (Transaction transaction in pool)
         {
            Transaction rawTransaction = transaction;

            int index = 0;
            foreach (TxOut output in rawTransaction.Outputs)
            {
               string[] addressIndex = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey);

               if (addressIndex == null)
                  continue;

               if (address == addressIndex.FirstOrDefault())
                  continue;

               string id = rawTransaction.GetHash().ToString();


               yield return new MapTransactionAddress
               {
                  Id = string.Format("{0}-{1}", id, index),
                  TransactionId = id,
                  Value = output.Value,
                  Index = index++,
                  Addresses = new List<string> { address },
                  ScriptHex = output.ScriptPubKey.ToHex(),
                  BlockIndex = -1,
                  CoinBase = rawTransaction.IsCoinBase,
                  CoinStake = rawTransaction.IsCoinStake,
               };
            }
         }
      }
      ///<Summary>
      /// Gets the transaction value and adds it to the balance of corresponding address in MapRichlist.
      /// If the address doesnt exist, it creates a new entry.
      ///</Summary>
      public void AddBalanceRichlist(MapTransactionAddress transaction)         
      {
         List<string> addresses = transaction.Addresses;
         long value = transaction.Value;

         foreach (string address in addresses)
         {           
            var data = new MapRichlist
            {
               Address = address,
               Balance = value,
            };
            FilterDefinition<MapRichlist> filter = Builders<MapRichlist>.Filter.Eq(address => address.Address, address);
            UpdateDefinition<MapRichlist> update = Builders<MapRichlist>.Update.Inc("Balance", value);
            
            if (MapRichlist.UpdateOne(filter, update).MatchedCount == 0)
            {
               MapRichlist.InsertOne(data);
            }            
         }         
      }

      ///<Summary>
      /// Gets the transaction value and substracts it from the balance of corresponding address in MapRichlist.
      ///</Summary>
      public void RemoveBalanceRichlist(MapTransactionAddress transaction)
      {
         string transactionhash = transaction.Id;
         SyncTransactionItems item = TransactionItemsGet(transactionhash.Split('-')[0]);
         if (item != null)
         {
            SyncTransactionItemOutput output = item.Outputs[Int32.Parse(transactionhash.Split('-')[1])];
            string address = output.Address;

            if (address != null)
            {
               long value = 0;

               if (output.SpentInTransaction != null)
               {
                  value = output.Value * -1;

               }
               var data = new MapRichlist
               {
                  Address = address,
                  Balance = value,
               };

               FilterDefinition<MapRichlist> filter = Builders<MapRichlist>.Filter.Eq(address => address.Address, address);
               UpdateDefinition<MapRichlist> update = Builders<MapRichlist>.Update.Inc("Balance", value);

               if (MapRichlist.UpdateOne(filter, update).MatchedCount == 0)
               {
                  MapRichlist.InsertOne(data);
               }
            }

         }

      }
   }
}
