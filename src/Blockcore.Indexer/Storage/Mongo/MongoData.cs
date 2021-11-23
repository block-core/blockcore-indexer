using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
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
using MongoDB.Bson;
using MongoDB.Driver;
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
      private readonly SyncingBlocks syncingBlocks;

      private readonly IndexerSettings configuration;

      private readonly ChainSettings chainConfiguration;

      private readonly System.Diagnostics.Stopwatch watch;

      public MongoData(ILogger<MongoStorageOperations> logger, SyncConnection connection, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainConfiguration, SyncingBlocks syncingBlocks)
      {
         configuration = nakoConfiguration.Value;
         this.chainConfiguration = chainConfiguration.Value;

         syncConnection = connection;
         this.syncingBlocks = syncingBlocks;
         log = logger;
         mongoClient = new MongoClient(configuration.ConnectionString.Replace("{Symbol}", this.chainConfiguration.Symbol.ToLower()));

         string dbName = configuration.DatabaseNameSubfix ? "Blockchain" + this.chainConfiguration.Symbol : "Blockchain";

         mongoDatabase = mongoClient.GetDatabase(dbName);
         MemoryTransactions = new ConcurrentDictionary<string, Transaction>();

         // Make sure we only create a single instance of the watcher.
         watch = Stopwatch.Start();
      }

      public List<IndexView> GetCurrentIndexes()
      {
            IMongoDatabase db = mongoClient.GetDatabase("admin");
            var command = new BsonDocument {
               { "currentOp", "1"},
            };
            BsonDocument currentOp = db.RunCommand<BsonDocument>(command);

            var inproc = currentOp.GetElement(0);
            var arr = inproc.Value as BsonArray;

            var ret = new List<IndexView>();

            foreach (BsonValue bsonValue in arr)
            {
               var desc = bsonValue.AsBsonDocument?.GetElement("desc");
               if (desc != null)
               {
                  bool track = desc?.Value.AsString.Contains("IndexBuildsCoordinatorMongod") ?? false;

                  if (track)
                  {
                     var indexed = new IndexView {Msg = bsonValue.AsBsonDocument?.GetElement("msg").Value.ToString()};

                     BsonElement? commandElement = bsonValue.AsBsonDocument?.GetElement("command");

                     string dbName = string.Empty;
                     if (commandElement.HasValue)
                     {
                        var bsn = commandElement.Value.Value.AsBsonDocument;
                        dbName = bsn.GetElement("$db").Value.ToString();
                        indexed.Command = $"{bsn.GetElement(0).Value}-{bsn.GetElement(1).Value}";
                     }

                     if (dbName == mongoDatabase.DatabaseNamespace.DatabaseName)
                     {
                        ret.Add(indexed);
                     }

                  }
               }
            }

            return ret;
      }

      public IMongoCollection<AddressForOutput> AddressForOutput
      {
         get
         {
            return mongoDatabase.GetCollection<AddressForOutput>("AddressForOutput");
         }
      }

      public IMongoCollection<AddressForInput> AddressForInput
      {
         get
         {
            return mongoDatabase.GetCollection<AddressForInput>("AddressForInput");
         }
      }

      public IMongoCollection<MapTransactionAddress> MapTransactionAddress
      {
         get
         {
            return mongoDatabase.GetCollection<MapTransactionAddress>("MapTransactionAddress");
         }
      }

      public IMongoCollection<AddressComputed> AddressComputed
      {
         get
         {
            return mongoDatabase.GetCollection<AddressComputed>("AddressComputed");
         }
      }

      public IMongoCollection<AddressHistoryComputed> AddressHistoryComputed
      {
         get
         {
            return mongoDatabase.GetCollection<AddressHistoryComputed>("AddressHistoryComputed");
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

      public ConcurrentDictionary<string, Transaction> MemoryTransactions { get; set; }

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
         // page using the block height as paging counter
         SyncBlockInfo storeTip = syncingBlocks.StoreTip;
         long total = storeTip?.BlockIndex ?? MapBlock.Find(Builders<MapBlock>.Filter.Empty).CountDocuments() - 1;

         if (total == -1) total = 0;

         if (offset == 0 || offset > total)
            offset = (int)total;

         IQueryable<MapBlock> filter = MapBlock.AsQueryable().Where(w => w.BlockIndex <= offset && w.BlockIndex > offset - limit);
         IEnumerable<SyncBlockInfo> list = filter.ToList().Select(Convert);

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

      public AddressForOutput GetTransactionOutput(string transaction, int index)
      {
         FilterDefinition<AddressForOutput> filter = Builders<AddressForOutput>.Filter.Eq(addr => addr.Outpoint, new Outpoint {TransactionId = transaction, OutputIndex = index});

         return AddressForOutput.Find(filter).ToList().FirstOrDefault();
      }

      public AddressForInput GetTransactionInput(string transaction, int index)
      {
         FilterDefinition<AddressForInput> filter = Builders<AddressForInput>.Filter.Eq(addr => addr.Outpoint, new Outpoint { TransactionId = transaction, OutputIndex = index });

         return AddressForInput.Find(filter).ToList().FirstOrDefault();
      }


      public MapTransactionAddress GetSpendingTransaction(string transaction, int index)
      {
         FilterDefinition<MapTransactionAddress> filter = Builders<MapTransactionAddress>.Filter.Eq(addr => addr.Id, string.Format("{0}-{1}", transaction, index));

         return MapTransactionAddress.Find(filter).ToList().FirstOrDefault();
      }

      public SyncTransactionInfo BlockTransactionGet(string transactionId)
      {
         FilterDefinition<MapTransactionBlock> filter = Builders<MapTransactionBlock>.Filter.Eq(info => info.TransactionId, transactionId);

         MapTransactionBlock trx = MapTransactionBlock.Find(filter).FirstOrDefault();
         if (trx == null)
         {
            return null;
         }

         SyncBlockInfo current = syncingBlocks.StoreTip;// GetLatestBlock();

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

      public SyncTransactionItems TransactionItemsGet(string transactionId, Transaction transaction = null)
      {
         if (transaction == null)
         {
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
               Address = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey)?.Addresses?.FirstOrDefault(),
               Index = index,
               Value = output.Value,
               OutputType = StandardScripts.GetTemplateFromScriptPubKey(output.ScriptPubKey)?.Type.ToString(),
               ScriptPubKey = output.ScriptPubKey.ToHex()
            }).ToList()
         };

         foreach (SyncTransactionItemInput input in ret.Inputs)
         {
            input.InputAddress = GetTransactionInput(input.PreviousTransactionHash, input.PreviousIndex)?.Address;
         }

         // try to fetch spent outputs
         foreach (SyncTransactionItemOutput output in ret.Outputs)
         {
            output.SpentInTransaction = GetTransactionInput(transactionId, output.Index)?.TrxHash;
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
            // If limit is higher than total, simply use offset 0 and get all that exists.
            if (limit > total)
            {
               offset = 1;
            }
            else
            {
               offset = (total - limit) + 1; // +1 to counteract the Skip -1 below.
            }
         }

         IEnumerable<MapRichlist> list = MapRichlist.Find(filter)
                   .SortBy(p => p.Balance)
                   .Skip(offset - 1) // 1 based index, so we'll subtract one.
                   .Limit(limit)
                   .ToList();

         return new QueryResult<MapRichlist> { Items = list, Total = total, Offset = offset, Limit = limit };
      }

      public MapRichlist RichlistBalance(string address)
      {
         FilterDefinitionBuilder<MapRichlist> filterBuilder = Builders<MapRichlist>.Filter;
         FilterDefinition<MapRichlist> filter = filterBuilder.Eq(m => m.Address, address);

         MapRichlist document = MapRichlist.Find(filter).SingleOrDefault();

         return document;
      }

      public List<MapRichlist> AddressBalances(IEnumerable<string> addresses)
      {
         FilterDefinitionBuilder<MapRichlist> filterBuilder = Builders<MapRichlist>.Filter;
         FilterDefinition<MapRichlist> filter = filterBuilder.Where(s => addresses.Contains(s.Address));

         List<MapRichlist> document = MapRichlist.Find(filter).ToList();

         return document;
      }

      public long TotalBalance()
      {
         FilterDefinitionBuilder<MapRichlist> builder = Builders<MapRichlist>.Filter;
         IQueryable<MapRichlist> filter = MapRichlist.AsQueryable();

         long totalBalance = filter.Sum(s => s.Balance);

         return totalBalance;
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
         SyncBlockInfo current = Blocks(0, 1).Items.FirstOrDefault();
         return current;
      }

      public QueryResult<QueryAddressItem> AddressHistory(string address, int offset, int limit)
      {
         // make sure fields are computed
         AddressComputed addressComputed = ComputeAddressBalance(address);

         IQueryable<AddressHistoryComputed> filter = AddressHistoryComputed.AsQueryable()
            .Where(t => t.Address == address);

         SyncBlockInfo storeTip = syncingBlocks.StoreTip;
         if (storeTip == null)
         {
            // this can happen if node is in the middle of reorg

            return new QueryResult<QueryAddressItem>
            {
               Items = Enumerable.Empty<QueryAddressItem>(),
               Offset = offset,
               Limit = limit,
               Total = 0
            };
         };

         // This will first perform one db query.
         long total = addressComputed.CountSent + addressComputed.CountReceived + addressComputed.CountStaked + addressComputed.CountMined;

         if (offset == 0 || offset > total)
            offset = (int)total;

         filter = filter.OrderByDescending(s => s.Position);

         // This will perform a query and return only transaction ID of the filtered results.
         //var list = filter.Skip(offset).Take(limit).ToList();

         var list = filter.Where(w => w.Position <= offset && w.Position > offset - limit).ToList();

         // Loop all transaction IDs and get the transaction object.
         IEnumerable<QueryAddressItem> transactions = list.Select(item => new QueryAddressItem
         {
            BlockIndex = item.BlockIndex,
            Value = item.AmountInOutputs - item.AmountInInputs,
            EntryType = item.EntryType,
            TransactionHash = item.TransactionId,
            Confirmations = syncingBlocks.StoreTip.BlockIndex + 1 - item.BlockIndex
         });

         return new QueryResult<QueryAddressItem>
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
      public QueryAddress AddressBalance(string address)
      {
         AddressComputed addressComputed = ComputeAddressBalance(address);

         return new QueryAddress
         {
            Address = address,
            Balance = addressComputed.Available,
            TotalReceived = addressComputed.Received,
            TotalStake = addressComputed.Staked,
            TotalMine = addressComputed.CountMined,
            TotalSent = addressComputed.Sent,
            TotalReceivedCount = addressComputed.CountReceived,
            TotalSentCount = addressComputed.CountSent,
            TotalStakeCount = addressComputed.CountStaked,
            TotalMineCount = addressComputed.CountMined
         };
      }

      /// <summary>
      /// Compute the balance and history of a given address.
      /// If the address already has history only the difference is computed.
      /// The difference is any new entries related to the given address from the last time it was computed.
      ///
      /// Edge cases that need special handling:
      /// - two inputs in the same transaction
      /// - to outputs in the same transaction
      /// - outputs and inputs in the same transaction
      ///
      /// Paging:
      /// We use a computed field called position that is incremented on each entry that is added to the list.
      /// The position is indexed but is only directly related to the given address
      /// When paging is requested we will fetch directly the required rows (no need to perform a table scan)
      ///
      /// Resource Access:
      /// concerns around computing tables
      ///    users call the method concurrently and compute the data simultaneously, this is mostly cpu wistful
      ///    as the tables are idempotent and the first call will compute and persist the computed data but second
      ///    will just fail to persist any existing entries, to apply this we use OCC (Optimistic Concurrency Control)
      ///    on the block height, if the version currently in disk is not the same as when the row was read
      ///    another process already calculated the latest additional entries
      /// </summary>
      private AddressComputed ComputeAddressBalance(string address)
      {
         if (syncingBlocks.IndexModeCompleted == false)
         {
            // do not compute tables if indexes have not run.
            throw new ApplicationException("node in syncing process");
         }

         FilterDefinition<AddressComputed> addrFilter = Builders<AddressComputed>.Filter
            .Where(f => f.Address == address);
         AddressComputed addressComputed = AddressComputed.Find(addrFilter).FirstOrDefault();

         if (addressComputed == null)
         {
            addressComputed = new AddressComputed() { Id = address, Address = address, ComputedBlockIndex = 0 };
            AddressComputed.ReplaceOne(addrFilter, addressComputed, new ReplaceOptions { IsUpsert = true });
         }

         SyncBlockInfo storeTip = syncingBlocks.StoreTip;
         if (storeTip == null)
            return addressComputed; // this can happen if node is in the middle of reorg

         long currentHeight = addressComputed.ComputedBlockIndex;
         long tipHeight = storeTip.BlockIndex;

         //IQueryable<MapTransactionAddress> filter = MapTransactionAddress.AsQueryable()
         //   .Where(t => t.Addresses.Contains(address))
         //   .Where(b => (b.BlockIndex > currentHeight && b.BlockIndex <= tipHeight)
         //               || (b.SpendingBlockIndex > currentHeight && b.SpendingBlockIndex <= tipHeight));

         IQueryable<AddressForOutput> filterOutputs = AddressForOutput.AsQueryable()
            .Where(t => t.Address == address)
            .Where(b => b.BlockIndex > currentHeight && b.BlockIndex <= tipHeight);

         IQueryable<AddressForInput> filterInputs = AddressForInput.AsQueryable()
            .Where(t => t.Address == address)
            .Where(b => b.BlockIndex > currentHeight && b.BlockIndex <= tipHeight);

         long countReceived = 0, countSent = 0, countStaked = 0, countMined = 0;
         long received = 0, sent = 0, staked = 0, mined = 0;
         long maxHeight = 0;

         var history = new Dictionary<string, AddressHistoryComputed>();
         var transcations = new Dictionary<string, MapAddressBag>();

         foreach (AddressForOutput item in filterOutputs)
         {
            if (item.BlockIndex > currentHeight && item.BlockIndex <= tipHeight)
            {
               maxHeight = Math.Max(maxHeight, item.BlockIndex);

               if (transcations.TryGetValue(item.Outpoint.TransactionId, out MapAddressBag current))
               {
                  current.CoinBase = item.CoinBase;
                  current.CoinStake = item.CoinStake;
                  current.Ouputs.Add(item);
               }
               else
               {
                  var bag = new MapAddressBag {BlockIndex = item.BlockIndex, CoinBase = item.CoinBase, CoinStake = item.CoinStake};
                  bag.Ouputs.Add(item);
                  transcations.Add(item.Outpoint.TransactionId, bag);
               }
            }
         }

         foreach (AddressForInput item in filterInputs)
         {
            if (item.BlockIndex > currentHeight && item.BlockIndex <= tipHeight)
            {
               maxHeight = Math.Max(maxHeight, item.BlockIndex);

               if (transcations.TryGetValue(item.TrxHash, out MapAddressBag current))
               {
                  current.Inputs.Add(item);
               }
               else
               {
                  var bag = new MapAddressBag { BlockIndex = item.BlockIndex };
                  bag.Inputs.Add(item);
                  transcations.Add(item.TrxHash, bag);
               }
            }
         }

         if (transcations.Any())
         {
            foreach (KeyValuePair<string, MapAddressBag> item in transcations.OrderBy(o => o.Value.BlockIndex))
            {
               var historyItem = new AddressHistoryComputed
               {
                  Address = addressComputed.Address,
                  TransactionId = item.Key,
                  BlockIndex = item.Value.BlockIndex,
                  Id = $"{item.Key}-{address}",
               };

               history.Add(item.Key, historyItem);

               foreach (AddressForOutput output in item.Value.Ouputs)
                  historyItem.AmountInOutputs += output.Value;

               foreach (AddressForInput output in item.Value.Inputs)
                  historyItem.AmountInInputs += output.Value;

               if (item.Value.CoinBase)
               {
                  countMined++;
                  mined += historyItem.AmountInOutputs;
                  historyItem.EntryType = "mine";
               }
               else if (item.Value.CoinStake)
               {
                  countStaked++;
                  staked += historyItem.AmountInOutputs - historyItem.AmountInInputs;
                  historyItem.EntryType = "stake";
               }
               else
               {
                  received += historyItem.AmountInOutputs;
                  sent += historyItem.AmountInInputs;

                  if (historyItem.AmountInOutputs > historyItem.AmountInInputs)
                  {
                     countReceived++;
                     historyItem.EntryType = "receive";
                  }
                  else
                  {
                     countSent++;
                     historyItem.EntryType = "send";
                  }
               }
            }

            long totalCount = countSent + countReceived + countMined + countStaked;
            if (totalCount < history.Values.Count)
            {
               throw new ApplicationException("Failed to compute history correctly");
            }

            // each entry is assigned an incremental id to improve efficiency of paging.
            long position = addressComputed.CountSent + addressComputed.CountReceived + addressComputed.CountStaked + addressComputed.CountMined;
            foreach (AddressHistoryComputed historyValue in history.Values.OrderBy(o => o.BlockIndex))
            {
               historyValue.Position = ++position;
            }

            addressComputed.Received += received;
            addressComputed.Staked += staked;
            addressComputed.Mined += mined;
            addressComputed.Sent += sent;
            addressComputed.Available = addressComputed.Received + addressComputed.Mined + addressComputed.Staked - addressComputed.Sent;
            addressComputed.CountReceived += countReceived;
            addressComputed.CountSent += countSent;
            addressComputed.CountStaked += countStaked;
            addressComputed.CountMined += countMined;
            addressComputed.ComputedBlockIndex = maxHeight; // the last block a trx was received to this address

            if (addressComputed.Available < 0)
            {
               throw new ApplicationException("Failed to compute balance correctly");
            }

            try
            {
               // only push to store if the same version of computed bloc index is present (meaning entry was not modified)
               // block height must change if new trx are added so use it to apply OCC (Optimistic Concurrency Control)
               // to determine if a newer entry was pushed to store.
               FilterDefinition<AddressComputed> updateFilter = Builders<AddressComputed>.Filter
                  .Where(f => f.Address == address && f.ComputedBlockIndex == currentHeight);

               // update the computed address entry, this will throw if a newer version is in store
               AddressComputed.ReplaceOne(updateFilter, addressComputed, new ReplaceOptions { IsUpsert = true });

               // if we managed to update the address we can safely insert history
               AddressHistoryComputed.InsertMany(history.Values, new InsertManyOptions { IsOrdered = false });
            }
            catch (MongoWriteException nwe)
            {
               if (nwe.WriteError.Category != ServerErrorCategory.DuplicateKey)
               {
                  throw;
               }

               // address was already modified fetch the latest version
               addressComputed = AddressComputed.Find(addrFilter).FirstOrDefault();
            }
            catch (MongoBulkWriteException mbwex)
            {
               // in cases of reorgs trx are not deleted from the store,
               // if a trx is already written and we attempt to write it again
               // the write will fail and throw, so we ignore such errors.
               // (IsOrdered = false will attempt all entries and only throw when done)
               if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))
               {
                  throw;
               }
            }
         }

         return addressComputed;
      }

      private class MapAddressBag
      {
         public long BlockIndex;
         public bool CoinBase;
         public bool CoinStake;

         public List<AddressForInput> Inputs = new List<AddressForInput>();
         public List<AddressForOutput> Ouputs = new List<AddressForOutput>();
      }

      public async Task DeleteBlockAsync(string blockHash)
      {
         SyncBlockInfo block = BlockByHash(blockHash);

         FilterDefinition<AddressForInput> addrForInputFilter = Builders<AddressForInput>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> input = AddressForInput.DeleteManyAsync(addrForInputFilter);

         FilterDefinition<AddressForOutput> addrForOutputFilter = Builders<AddressForOutput>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> output = AddressForOutput.DeleteManyAsync(addrForOutputFilter);

         // delete the transaction
         FilterDefinition<MapTransactionBlock> transactionFilter = Builders<MapTransactionBlock>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         Task<DeleteResult> transactions = MapTransactionBlock.DeleteManyAsync(transactionFilter);

         // delete computed
         FilterDefinition<AddressComputed> addrCompFilter = Builders<AddressComputed>.Filter.Eq(addr => addr.ComputedBlockIndex, block.BlockIndex);
         Task<DeleteResult> addressComputed = AddressComputed.DeleteManyAsync(addrCompFilter);

         // delete computed history
         FilterDefinition<AddressHistoryComputed> addrCompHistFilter = Builders<AddressHistoryComputed>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> addressHistoryComputed = AddressHistoryComputed.DeleteManyAsync(addrCompHistFilter);

         await Task.WhenAll(input, output, transactions, addressComputed, addressHistoryComputed);

         // delete the block itself is done last
         FilterDefinition<MapBlock> blockFilter = Builders<MapBlock>.Filter.Eq(info => info.BlockHash, blockHash);
         MapBlock.DeleteOne(blockFilter);
      }

      public QueryResult<QueryTransaction> GetMemoryTransactions(int offset, int limit)
      {
         ICollection<Transaction> list = MemoryTransactions.Values;

         List<QueryTransaction> retList = new List<QueryTransaction>();

         foreach (Transaction trx in list.Skip(offset - 1).Take(limit)) // 1 based index, so we'll subtract one.
         {
            string transactionId = trx.GetHash().ToString();
            SyncTransactionItems transactionItems = TransactionItemsGet(transactionId, trx);

            var result = new QueryTransaction
            {
               Symbol = chainConfiguration.Symbol,
               Confirmations = 0,
               TransactionId = transactionId,

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

            retList.Add(result);
         }

         var queryResult = new QueryResult<QueryTransaction>
         {
            Items = retList,
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

      private AddressBalance GetTransactionsByAddress(long confirmations, long blockIndex, string address)
      {
         // Create a query against transactions on the specified address.
         IQueryable<MapTransactionAddress> filter = null;// AddressTransactionFilter(address, 0);

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

      private IEnumerable<Tuple<TxIn, string>> GetPoolOutputs(IEnumerable<Transaction> pool)
      {
         return pool.SelectMany(s => s.Inputs.Select(v => new Tuple<TxIn, string>(v, s.GetHash().ToString())));
      }

      private IEnumerable<MapTransactionAddress> PoolToMapTransactionAddress(IEnumerable<Transaction> pool, string address)
      {
         foreach (Transaction transaction in pool)
         {
            Transaction rawTransaction = transaction;

            int index = 0;
            foreach (TxOut output in rawTransaction.Outputs)
            {
               string[] addressIndex = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey)?.Addresses;

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
