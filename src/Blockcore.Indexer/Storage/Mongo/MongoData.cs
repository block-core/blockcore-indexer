using Blockcore.Indexer.Crypto;

namespace Blockcore.Indexer.Storage.Mongo
{
   using System;
   using System.Collections.Concurrent;
   using System.Collections.Generic;
   using System.Linq;
   using Blockcore.Indexer.Client;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage.Mongo.Types;
   using Blockcore.Indexer.Storage.Types;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;
   using MongoDB.Driver;
   using NBitcoin;
   using NBitcoin.DataEncoders;

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

      public ConcurrentDictionary<string, NBitcoin.Transaction> MemoryTransactions { get; set; }

      public IEnumerable<SyncBlockInfo> BlockGetIncompleteBlocks()
      {
         // note this field is not indexed
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(info => info.SyncComplete, false);

         return MapBlock.Find(filter).ToList().Select(Convert);
      }

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

      public SyncBlockInfo BlockGetByIndex(long blockIndex)
      {
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(info => info.BlockIndex, blockIndex);

         return MapBlock.Find(filter).ToList().Select(Convert).FirstOrDefault();
      }

      public SyncBlockInfo BlockGetByHash(string blockHash)
      {
         FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(info => info.BlockHash, blockHash);

         return MapBlock.Find(filter).ToList().Select(Convert).FirstOrDefault();
      }

      public void InsertBlock(MapBlock info)
      {
         MapBlock.InsertOne(info);
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

         SyncBlockInfo current = BlockGetBlockCount(1).First();

         SyncBlockInfo blk = BlockGetByIndex(trx.BlockIndex);

         return new SyncTransactionInfo
         {
            BlockIndex = trx.BlockIndex,
            BlockHash = blk.BlockHash,
            Timestamp = blk.BlockTime,
            TransactionHash = trx.TransactionId,
            Confirmations = current.BlockIndex - trx.BlockIndex
         };
      }

      public IEnumerable<SyncTransactionInfo> BlockTransactionGetByBlock(string blockHash)
      {
         SyncBlockInfo blk = BlockGetByHash(blockHash);
         SyncBlockInfo current = BlockGetBlockCount(1).First();

         FilterDefinition<MapTransactionBlock> filter = Builders<MapTransactionBlock>.Filter.Eq(info => info.BlockIndex, blk.BlockIndex);
         var trxs = MapTransactionBlock.Find(filter).ToList();

         return trxs.Select(s => new SyncTransactionInfo
         {
            BlockIndex = s.BlockIndex,
            BlockHash = blk.BlockHash,
            Timestamp = blk.BlockTime,
            TransactionHash = s.TransactionId,
            Confirmations = current.BlockIndex - s.BlockIndex
         });
      }

      public IEnumerable<SyncTransactionInfo> BlockTransactionGetByBlockIndex(long blockIndex)
      {
         SyncBlockInfo blk = BlockGetByIndex(blockIndex);
         SyncBlockInfo current = BlockGetBlockCount(1).First();

         FilterDefinition<MapTransactionBlock> filter = Builders<MapTransactionBlock>.Filter.Eq(info => info.BlockIndex, blk.BlockIndex);
         var trxs = MapTransactionBlock.Find(filter).ToList();

         return trxs.Select(s => new SyncTransactionInfo
         {
            BlockIndex = s.BlockIndex,
            BlockHash = blk.BlockHash,
            Timestamp = blk.BlockTime,
            TransactionHash = s.TransactionId,
            Confirmations = current.BlockIndex - s.BlockIndex
         });
      }

      public SyncTransactionItemOutput TransactionsGet(string transactionId, int index, SyncTransactionIndexType indexType)
      {
         throw new NotImplementedException();
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

      public SyncTransactionAddressBalance AddressGetBalance(string address, long confirmations)
      {
         SyncBlockInfo current = BlockGetBlockCount(1).First();

         var addrs = SelectAddressWithPool(current, address, false).ToList();
         return CreateAddresBalance(confirmations, addrs, false);
      }

      public SyncTransactionAddressBalance AddressGetBalanceUtxo(string address, long confirmations)
      {
         SyncBlockInfo current = BlockGetBlockCount(1).First();

         var addrs = SelectAddressWithPool(current, address, true).ToList();
         return CreateAddresBalance(confirmations, addrs, true);
      }

      public void DeleteBlock(string blockHash)
      {
         SyncBlockInfo block = BlockGetByHash(blockHash);

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

      public IEnumerable<NBitcoin.Transaction> GetMemoryTransactions()
      {
         return MemoryTransactions.Values;
      }

      private SyncTransactionAddressBalance CreateAddresBalance(long confirmations, List<SyncTransactionAddressItem> addrs, bool availableOnly)
      {
         long all = addrs.Where(s => s.Confirmations >= confirmations).Sum(s => s.Value);
         long confirming = addrs.Where(s => s.Confirmations < confirmations).Sum(s => s.Value);
         long used = addrs.Where(s => s.SpendingTransactionHash != null).Sum(s => s.Value);
         long available = all - used;

         return new SyncTransactionAddressBalance
         {
            Available = available,
            Received = availableOnly ? default(long?) : all,
            Sent = availableOnly ? default(long?) : used,
            Unconfirmed = confirming,
            Items = addrs
         };
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

         log.LogInformation($"Select: Seconds = {watch.Elapsed.TotalSeconds} - UnspentOnly = {availableOnly} - Addr = {address} - Items = {addrs.Count()}");

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
            Time = s.BlockIndex == -1 ? UnixUtils.DateToUnixTimestamp(DateTime.UtcNow) : current.BlockTime
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

      public class SelectBalanceResult
      {
         public double TotalAmount { get; set; }

         public int Count { get; set; }

         public SelectStats id { get; set; }

         public class SelectStats
         {
            public bool Spent { get; set; }
            public bool Confirmed { get; set; }
         }
      }
   }
}
