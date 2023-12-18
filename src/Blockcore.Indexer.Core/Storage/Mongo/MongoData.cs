using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Blockcore.NBitcoin.DataEncoders;
using Blockcore.Utilities;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class MongoData : IStorage
   {
      private readonly ILogger<MongoDb> log;
      private readonly IMongoDb mongoDb;
      private readonly IMongoDatabase mongoDatabase;
      private readonly SyncConnection syncConnection;
      private readonly GlobalState globalState;
      private readonly ChainSettings chainConfiguration;

      private readonly IScriptInterpreter scriptInterpeter;

      private readonly IMapMongoBlockToStorageBlock mongoBlockToStorageBlock;
      readonly ICryptoClientFactory clientFactory;

      readonly IBlockRewindOperation rewindOperation;

      readonly IComputeHistoryQueue computeHistoryQueue;

      public MongoData(ILogger<MongoDb> dbLogger, SyncConnection connection, IOptions<ChainSettings> chainConfiguration,
         GlobalState globalState, IMapMongoBlockToStorageBlock mongoBlockToStorageBlock, ICryptoClientFactory clientFactory,
         IScriptInterpreter scriptInterpeter, IMongoDatabase mongoDatabase, IMongoDb db, IBlockRewindOperation rewindOperation, IComputeHistoryQueue computeHistoryQueue)
      {
         log = dbLogger;
         this.chainConfiguration = chainConfiguration.Value;
         this.globalState = globalState;
         syncConnection = connection;

         this.mongoBlockToStorageBlock = mongoBlockToStorageBlock;
         this.clientFactory = clientFactory;
         this.scriptInterpeter = scriptInterpeter;
         this.mongoDatabase = mongoDatabase;
         mongoDb = db;
         this.rewindOperation = rewindOperation;
         this.computeHistoryQueue = computeHistoryQueue;
      }

      /// <summary>
      /// Return all the indexes that index the BlockIndex parameter.
      /// Rewind logic uses BlockIndex to revert the chain those are the indexes we want to see built.
      /// </summary>
      /// <returns></returns>
      public List<string> GetBlockIndexIndexes()
      {
         List<string> collections = mongoDatabase.ListCollectionNames().ToList();

         List<string> indexNames = new();

         foreach (string colName in collections)
         {
            var indexes = mongoDatabase.GetCollection<BsonDocument>(colName, new MongoCollectionSettings { }).Indexes.List().ToList();

            foreach (string indexName in indexes.Select(s => colName + " - " + s.ToString()))
            {
               indexNames.Add(indexName);
            }
         }

         return indexNames.Where(w => w.Contains("BlockIndex")).ToList();
      }

      public List<IndexView> GetIndexesBuildProgress()
      {
            IMongoDatabase db = mongoDatabase.Client.GetDatabase("admin");
            var command = new BsonDocument {
               { "currentOp", "1"},
            };
            BsonDocument currentOp = db.RunCommand<BsonDocument>(command);

            BsonElement inproc = currentOp.GetElement(0);
            var arr = inproc.Value as BsonArray;

            var ret = new List<IndexView>();

            foreach (BsonValue bsonValue in arr)
            {
               BsonElement? desc = bsonValue.AsBsonDocument?.GetElement("desc");
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
                        BsonDocument bsn = commandElement.Value.Value.AsBsonDocument;
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
            TransactionIndex = transaction?.TransactionIndex,
            RBF = transactionItems.RBF,
            LockTime = transactionItems.LockTime.ToString(),
            Version = transactionItems.Version,
            IsCoinbase = transactionItems.IsCoinbase,
            IsCoinstake = transactionItems.IsCoinstake,
            Fee = transactionItems.Fee,
            Weight = transactionItems.Weight,
            Size = transactionItems.Size,
            VirtualSize = transactionItems.VirtualSize,
            HasWitness = transactionItems.HasWitness,
            Inputs = transactionItems.Inputs.Select(i => new QueryTransactionInput
            {
               CoinBase = i.InputCoinBase,
               InputAddress = i.InputAddress,
               InputAmount = i.InputAmount,
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
      public QueryResult<SyncBlockInfo> Blocks(int? offset, int limit)
      {
         SyncBlockInfo storeTip = globalState.StoreTip;
         long index = storeTip?.BlockIndex ?? mongoDb.BlockTable.Find(Builders<BlockTable>.Filter.Empty).CountDocuments() - 1;

         // Get the total number of items based off the index.
         long total = index + 1;

         // If the offset has value, then use it, if not fetch the latest blocks.
         long startPosition = offset ?? total - limit;
         long endPosition = startPosition + limit;

         // The BlockIndex is 0 based, so we must perform >= to get first.
         IQueryable<BlockTable> filter = mongoDb.BlockTable.AsQueryable().OrderBy(b => b.BlockIndex).Where(w => w.BlockIndex >= startPosition && w.BlockIndex < endPosition);

         IEnumerable<SyncBlockInfo> list = filter.ToList().Select(mongoBlockToStorageBlock.Map);

         return new QueryResult<SyncBlockInfo> { Items = list, Total = total, Offset = (int)startPosition, Limit = limit };
      }

      public SyncBlockInfo GetLatestBlock()
      {
         if (globalState.StoreTip != null)
            return globalState.StoreTip;

         BlockTable recentBlock = mongoDb.BlockTable.AsQueryable().OrderByDescending(a => a.BlockIndex).FirstOrDefault();

         if (recentBlock == null)
            return null;

         return mongoBlockToStorageBlock.Map(recentBlock);
      }

      public SyncBlockInfo BlockByIndex(long blockIndex)
      {
         FilterDefinition<BlockTable> filter = Builders<BlockTable>.Filter.Eq(info => info.BlockIndex, blockIndex);

         SyncBlockInfo block = mongoDb.BlockTable.Find(filter).ToList().Select(mongoBlockToStorageBlock.Map).FirstOrDefault();

         SyncBlockInfo tip = globalState.StoreTip;

         if (tip != null && block != null)
            block.Confirmations = tip.BlockIndex + 1 - block.BlockIndex;

         return block;
      }

      public SyncBlockInfo BlockByHash(string blockHash)
      {
         FilterDefinition<BlockTable> filter = Builders<BlockTable>.Filter.Eq(info => info.BlockHash, blockHash);

         SyncBlockInfo block = mongoDb.BlockTable.Find(filter).ToList().Select(mongoBlockToStorageBlock.Map).FirstOrDefault();

         SyncBlockInfo tip = globalState.StoreTip;

         if (tip != null && block != null)
            block.Confirmations = tip.BlockIndex + 1 - block.BlockIndex;

         return block;
      }

      public QueryResult<QueryOrphanBlock> OrphanBlocks(int? offset, int limit)
      {
         int total = (int)mongoDb.ReorgBlock.EstimatedDocumentCount();

         int itemsToSkip = offset ?? (total < limit ? 0 : total - limit);

         ICollection<ReorgBlockTable> list = mongoDb.ReorgBlock
            .AsQueryable()
            .OrderBy(o => o.BlockIndex)
            .Skip(itemsToSkip)
            .Take(limit)
            .ToList();

         return new QueryResult<QueryOrphanBlock>
         {
            Items = list.Select(s => new QueryOrphanBlock
            {
               BlockHash = s.BlockHash,
               BlockIndex = s.BlockIndex,
               Created = s.Created,
               Block = s.Block
            }),
            Total = total,
            Offset = itemsToSkip,
            Limit = limit
         };
      }

      public ReorgBlockTable OrphanBlockByHash(string blockHash)
      {
         FilterDefinition<ReorgBlockTable> filter = Builders<ReorgBlockTable>.Filter.Eq(info => info.BlockHash, blockHash);

         return mongoDb.ReorgBlock.Find(filter).ToList().FirstOrDefault();
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

         ReplaceOneResult replaceOneResult = await mongoDb.Peer.ReplaceOneAsync(doc => doc.Addr == info.Addr, info, new ReplaceOptions { IsUpsert = true });

         return replaceOneResult.ModifiedCount;
      }

      public List<PeerInfo> GetPeerFromDate(DateTime date)
      {
         FilterDefinition<PeerInfo> filter = Builders<PeerInfo>.Filter.Gt(addr => addr.LastSeen, date);
         return mongoDb.Peer.Find(filter).ToList();
      }

      public SyncRawTransaction TransactionGetByHash(string trxHash)
      {
         FilterDefinition<TransactionTable> filter = Builders<TransactionTable>.Filter.Eq(info => info.TransactionId, trxHash);

         return mongoDb.TransactionTable.Find(filter).ToList().Select(t => new SyncRawTransaction { TransactionHash = trxHash, RawTransaction = t.RawTransaction }).FirstOrDefault();
      }

      public InputTable GetTransactionInput(string transaction, int index)
      {
         FilterDefinition<InputTable> filter = Builders<InputTable>.Filter.Eq(addr => addr.Outpoint, new Outpoint { TransactionId = transaction, OutputIndex = index });

         return mongoDb.InputTable.Find(filter).ToList().FirstOrDefault();
      }

      public OutputTable GetTransactionOutput(string transaction, int index)
      {
         FilterDefinition<OutputTable> filter = Builders<OutputTable>.Filter.Eq(addr => addr.Outpoint, new Outpoint { TransactionId = transaction, OutputIndex = index });

         return mongoDb.OutputTable.Find(filter).ToList().FirstOrDefault();
      }

      public SyncTransactionInfo BlockTransactionGet(string transactionId)
      {
         FilterDefinition<TransactionBlockTable> filter = Builders<TransactionBlockTable>.Filter.Eq(info => info.TransactionId, transactionId);

         TransactionBlockTable trx = mongoDb.TransactionBlockTable.Find(filter).FirstOrDefault();
         if (trx == null)
         {
            return null;
         }

         SyncBlockInfo current = globalState.StoreTip;// GetLatestBlock();

         SyncBlockInfo blk = BlockByIndex(trx.BlockIndex);

         return new SyncTransactionInfo
         {
            BlockIndex = trx.BlockIndex,
            BlockHash = blk.BlockHash,
            Timestamp = blk.BlockTime,
            TransactionHash = trx.TransactionId,
            TransactionIndex = trx.TransactionIndex,
            Confirmations = current.BlockIndex + 1 - trx.BlockIndex
         };
      }

      public string GetRawTransaction(string transactionId)
      {
         // Try to find the trx in disk
         SyncRawTransaction rawtrx = TransactionGetByHash(transactionId);

         if (rawtrx != null)
         {
            return Encoders.Hex.EncodeData(rawtrx.RawTransaction);
         }

         IBlockchainClient client = clientFactory.Create(syncConnection);

         Client.Types.DecodedRawTransaction res = client.GetRawTransactionAsync(transactionId, 0).Result;

         if (res.Hex != null)
         {
            return res.Hex;
         }

         return null;
      }

      public string GetRawBlock(string blockHash)
      {
         IBlockchainClient client = clientFactory.Create(syncConnection);

         string res = client.GetBlockHex(blockHash);

         return res;
      }


      public SyncTransactionItems TransactionItemsGet(string transactionId, Transaction transaction = null)
      {

         if (transaction == null)
         {
            // Try to find the trx in disk
            SyncRawTransaction rawtrx = TransactionGetByHash(transactionId);

            if (rawtrx == null)
            {
               var client = clientFactory.Create(syncConnection);

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

         bool hasWitness = transaction.HasWitness;
         int witnessScaleFactor = syncConnection.Network.Consensus.Options?.WitnessScaleFactor ?? 4;

         int size = NBitcoin.BitcoinSerializableExtensions.GetSerializedSize(transaction, syncConnection.Network.Consensus.ConsensusFactory) ;
         int virtualSize = hasWitness ? transaction.GetVirtualSize(witnessScaleFactor) : size;
         int weight = virtualSize * witnessScaleFactor - (witnessScaleFactor - 1);

         var ret = new SyncTransactionItems
         {
            RBF = transaction.RBF,
            LockTime = transaction.LockTime.ToString(),
            Version = transaction.Version,
            HasWitness = hasWitness,
            Size = size,
            VirtualSize = virtualSize,
            Weight = weight,
            IsCoinbase = transaction.IsCoinBase,
            IsCoinstake = syncConnection.Network.Consensus.IsProofOfStake && transaction.IsCoinStake,
            Inputs = transaction.Inputs.Select(v => new SyncTransactionItemInput
            {
               PreviousTransactionHash = v.PrevOut.Hash.ToString(),
               PreviousIndex = (int)v.PrevOut.N,
               WitScript = v.WitScript.ToScript().ToHex(),
               ScriptSig = v.ScriptSig.ToHex(),
               InputAddress = scriptInterpeter.GetSignerAddress(syncConnection.Network, v.ScriptSig),
               SequenceLock = v.Sequence.ToString(),
            }).ToList(),
            Outputs = transaction.Outputs.Select((output, index) => new SyncTransactionItemOutput
            {
               Address = scriptInterpeter.InterpretScript(syncConnection.Network, output.ScriptPubKey)?.Addresses?.FirstOrDefault(),
               Index = index,
               Value = output.Value,
               OutputType = scriptInterpeter.InterpretScript(syncConnection.Network, output.ScriptPubKey)?.ScriptType, // StandardScripts.GetTemplateFromScriptPubKey(output.ScriptPubKey)?.Type.ToString(),
               ScriptPubKey = output.ScriptPubKey.ToHex()
            }).ToList()
         };

         foreach (SyncTransactionItemInput input in ret.Inputs)
         {
            OutputTable outputTable = GetTransactionOutput(input.PreviousTransactionHash, input.PreviousIndex);
            input.InputAddress = outputTable?.Address;
            input.InputAmount = outputTable?.Value ?? 0;
         }

         // try to fetch spent outputs
         foreach (SyncTransactionItemOutput output in ret.Outputs)
         {
            output.SpentInTransaction = GetTransactionInput(transactionId, output.Index)?.TrxHash;
         }

         if (!ret.IsCoinbase && !ret.IsCoinstake)
         {
            // calcualte fee and feePk
            ret.Fee = ret.Inputs.Sum(s => s.InputAmount) - ret.Outputs.Sum(s => s.Value);
         }

         return ret;
      }

      public QueryResult<RichlistTable> Richlist(int offset, int limit)
      {
         FilterDefinitionBuilder<RichlistTable> filterBuilder = Builders<RichlistTable>.Filter;
         FilterDefinition<RichlistTable> filter = filterBuilder.Empty;

         // Skip and Limit only supports int, so we can't support long amount of documents.
         int total = (int)mongoDb.RichlistTable.Find(filter).CountDocuments();

         // If the offset is not set, or set to 0 implicit, we'll reverse the query and grab last page as oppose to first.
         //if (offset == 0)
         //{
         //   // If limit is higher than total, simply use offset 0 and get all that exists.
         //   if (limit > total)
         //   {
         //      offset = 1;
         //   }
         //   else
         //   {
         //      offset = (total - limit); // +1 to counteract the Skip -1 below.
         //   }
         //}

         IEnumerable<RichlistTable> list = mongoDb.RichlistTable.Find(filter)
                   .SortByDescending(p => p.Balance)
                   .Skip(offset) // 1 based index, so we'll subtract one.
                   .Limit(limit)
                   .ToList();

         return new QueryResult<RichlistTable> { Items = list, Total = total, Offset = offset, Limit = limit };
      }

      public RichlistTable RichlistBalance(string address)
      {
         FilterDefinitionBuilder<RichlistTable> filterBuilder = Builders<RichlistTable>.Filter;
         FilterDefinition<RichlistTable> filter = filterBuilder.Eq(m => m.Address, address);

         RichlistTable table = mongoDb.RichlistTable.Find(filter).SingleOrDefault();

         return table;
      }

      public List<RichlistTable> AddressBalances(IEnumerable<string> addresses)
      {
         FilterDefinitionBuilder<RichlistTable> filterBuilder = Builders<RichlistTable>.Filter;
         FilterDefinition<RichlistTable> filter = filterBuilder.Where(s => addresses.Contains(s.Address));

         List<RichlistTable> document = mongoDb.RichlistTable.Find(filter).ToList();

         return document;
      }

      public long TotalBalance()
      {
         FilterDefinitionBuilder<RichlistTable> builder = Builders<RichlistTable>.Filter;
         IQueryable<RichlistTable> filter = mongoDb.RichlistTable.AsQueryable();

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

         if (blk == null)
         {
            return null;
         }

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
         FilterDefinition<TransactionBlockTable> filter = Builders<TransactionBlockTable>.Filter.Eq(info => info.BlockIndex, index);

         int total = (int)mongoDb.TransactionBlockTable.Find(filter).CountDocuments();

         IEnumerable<SyncTransactionInfo> list = mongoDb.TransactionBlockTable.Find(filter)
                   .SortBy(p => p.TransactionIndex)
                   .Skip(offset)
                   .Limit(limit)
                   .ToList().Select(s => new SyncTransactionInfo
                   {
                      TransactionHash = s.TransactionId,
                      TransactionIndex = s.TransactionIndex,
                   });

         return new QueryResult<SyncTransactionInfo>
         {
            Items = list,
            Offset = offset,
            Limit = limit,
            Total = total
         };
      }

      public QueryResult<QueryAddressItem> AddressHistory(string address, int? offset, int limit)
      {
         // make sure fields are computed
         AddressComputedTable addressComputedTable = ComputeAddressBalance(address);

         IQueryable<AddressHistoryComputedTable> filter = mongoDb.AddressHistoryComputedTable.AsQueryable()
            .Where(t => t.Address == address);

         SyncBlockInfo storeTip = globalState.StoreTip;
         if (storeTip == null)
         {
            // this can happen if node is in the middle of reorg

            return new QueryResult<QueryAddressItem>
            {
               Items = Enumerable.Empty<QueryAddressItem>(),
               Offset = 0,
               Limit = limit,
               Total = 0
            };
         };

         // This will first perform one db query.
         long total = addressComputedTable.CountSent + addressComputedTable.CountReceived + addressComputedTable.CountStaked + addressComputedTable.CountMined;

         // Filter by the position, in the order of first entry being 1 and then second entry being 2.
         filter = filter.OrderBy(s => s.Position);

         long startPosition = offset ?? total - limit;
         long endPosition = (startPosition) + limit;

         // Get all items that is higher than start position and lower than end position.
         var list = filter.Where(w => w.Position > startPosition && w.Position <= endPosition).ToList();

         // Loop all transaction IDs and get the transaction object.
         IEnumerable<QueryAddressItem> transactions = list.Select(item => new QueryAddressItem
         {
            BlockIndex = item.BlockIndex,
            Value = item.AmountInOutputs - item.AmountInInputs,
            EntryType = item.EntryType,
            TransactionHash = item.TransactionId,
            Confirmations = storeTip.BlockIndex + 1 - item.BlockIndex
         });

         IEnumerable<QueryAddressItem> mempollTransactions = null;

         if (offset == total)
         {
            List<MapMempoolAddressBag> mempoolAddressBag = MempoolBalance(address);

            mempollTransactions = mempoolAddressBag.Select(item => new QueryAddressItem
            {
               BlockIndex = 0,
               Value = item.AmountInOutputs - item.AmountInInputs,
               EntryType = item.AmountInOutputs > item.AmountInInputs ? "receive" : "send",
               TransactionHash = item.Mempool.TransactionId,
               Confirmations = 0
            });
         }

         List<QueryAddressItem> allTransactions = new();

         if (mempollTransactions != null)
            allTransactions.AddRange(mempollTransactions);

         allTransactions.AddRange(transactions);

         return new QueryResult<QueryAddressItem>
         {
            Items = allTransactions,
            Offset = (int)startPosition,
            Limit = limit,
            Total = total
         };
      }

      /// <summary>
      /// Calculates the balance for specified address.
      /// </summary>
      /// <param name="address"></param>
      public QueryAddress AddressBalance(string address)
      {
         AddressComputedTable addressComputedTable = ComputeAddressBalance(address);

         List<MapMempoolAddressBag> mempoolAddressBag = MempoolBalance(address);

         return new QueryAddress
         {
            Address = address,
            Balance = addressComputedTable.Available,
            TotalReceived = addressComputedTable.Received,
            TotalStake = addressComputedTable.Staked,
            TotalMine = addressComputedTable.Mined,
            TotalSent = addressComputedTable.Sent,
            TotalReceivedCount = addressComputedTable.CountReceived,
            TotalSentCount = addressComputedTable.CountSent,
            TotalStakeCount = addressComputedTable.CountStaked,
            TotalMineCount = addressComputedTable.CountMined,
            PendingSent = mempoolAddressBag.Sum(s => s.AmountInInputs),
            PendingReceived = mempoolAddressBag.Sum(s => s.AmountInOutputs)
         };
      }

      public async Task<List<QueryAddressBalance>> QuickBalancesLookupForAddressesWithHistoryCheckAsync(IEnumerable<string> addresses, bool includePending = false)
      {
         var outputTask = mongoDb.OutputTable.Distinct(_ => _.Address, _ => addresses.Contains(_.Address))
            .ToListAsync();

         var utxoBalances = mongoDb.UnspentOutputTable.Aggregate()
            .Match(_ => addresses.Contains(_.Address))
            .Group(_ => _.Address,
               _ => new { Address = _.Key, Balance = _.Sum(utxo => utxo.Value) })
            .ToList();

         await outputTask;

         var results = outputTask.Result.Select(_ =>
         {
            var balance = new QueryAddressBalance
            {
               Address = _,
               Balance = utxoBalances.FirstOrDefault(u => u.Address.Equals(_))?.Balance ?? 0
            };
            return balance;
         }).ToList();

         if (includePending)
         {
            var pending = addresses.Select(_ =>
            {
               List<MapMempoolAddressBag> mempoolAddressBag = MempoolBalance(_);

               return new QueryAddressBalance { Address = _, PendingSent = mempoolAddressBag.Sum(s => s.AmountInInputs), PendingReceived = mempoolAddressBag.Sum(s => s.AmountInOutputs) };
            });

            foreach (var items in pending)
            {
               if (items.PendingReceived > 0 || items.PendingSent > 0)
               {
                  var item = results.FirstOrDefault(_ => _.Address == items.Address);

                  if (item == null)
                  {
                     results.Add(items);
                  }
                  else
                  {
                     item.PendingReceived = items.PendingReceived;
                     item.PendingSent = items.PendingSent;
                  }
               }
            }
         }

         results.ForEach(_ => computeHistoryQueue.AddAddressToComputeHistoryQueue(_.Address));

         return results;
      }

      private List<MapMempoolAddressBag> MempoolBalance(string address)
      {
         var mapMempoolAddressBag = new List<MapMempoolAddressBag>();

         if (globalState.LocalMempoolView.IsEmpty)
            return mapMempoolAddressBag;

         var mempoolForAddress = mongoDb.Mempool
            .Aggregate()
            .Match(m => m.AddressInputs.Contains(address) || m.AddressOutputs.Contains(address))
            .ToList();

         foreach (MempoolTable mempool in mempoolForAddress)
         {
            var bag = new MapMempoolAddressBag { Mempool = mempool };

            foreach (MempoolOutput mempoolOutput in mempool.Outputs)
            {
               if (mempoolOutput.Address == address)
                  bag.AmountInOutputs += mempoolOutput.Value;
            }

            foreach (MempoolInput mempoolInput in mempool.Inputs)
            {
               if (mempoolInput.Address == address)
                  bag.AmountInInputs += mempoolInput.Value;
            }

            mapMempoolAddressBag.Add(bag);
         }

         return mapMempoolAddressBag;
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
      private AddressComputedTable ComputeAddressBalance(string address)
      {
         //if (globalState.IndexModeCompleted == false)
         //{
         //   // do not compute tables if indexes have not run.
         //   throw new ApplicationException("node in syncing process");
         //}

         FilterDefinition<AddressComputedTable> addrFilter = Builders<AddressComputedTable>.Filter
            .Where(f => f.Address == address);
         AddressComputedTable addressComputedTable = mongoDb.AddressComputedTable.Find(addrFilter).FirstOrDefault();

         if (addressComputedTable == null)
         {
            addressComputedTable = new AddressComputedTable() { Id = address, Address = address, ComputedBlockIndex = 0 };
            mongoDb.AddressComputedTable.ReplaceOne(addrFilter, addressComputedTable, new ReplaceOptions { IsUpsert = true });
         }

         SyncBlockInfo storeTip = globalState.StoreTip;
         if (storeTip == null)
            return addressComputedTable; // this can happen if node is in the middle of reorg

         long currentHeight = addressComputedTable.ComputedBlockIndex;
         long tipHeight = storeTip.BlockIndex;

         IQueryable<OutputTable> filterOutputs = mongoDb.OutputTable.AsQueryable()
            .Where(t => t.Address == address)
            .Where(b => b.BlockIndex > currentHeight && b.BlockIndex <= tipHeight);

         IQueryable<InputTable> filterInputs = mongoDb.InputTable.AsQueryable()
            .Where(t => t.Address == address)
            .Where(b => b.BlockIndex > currentHeight && b.BlockIndex <= tipHeight);

         long countReceived = 0, countSent = 0, countStaked = 0, countMined = 0;
         long received = 0, sent = 0, staked = 0, mined = 0;
         long maxHeight = 0;

         var history = new Dictionary<string, AddressHistoryComputedTable>();
         var transcations = new Dictionary<string, MapAddressBag>();
         var utxoToAdd = new Dictionary<string, AddressUtxoComputedTable>();
         var utxoToDelete = new Dictionary<string, Outpoint>();

         foreach (OutputTable item in filterOutputs)
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

               // add to the utxo table
               utxoToAdd.Add(item.Outpoint.ToString(), new AddressUtxoComputedTable {Outpoint = item.Outpoint, BlockIndex = item.BlockIndex, Address = item.Address, CoinBase = item.CoinBase, CoinStake = item.CoinStake, ScriptHex = item.ScriptHex, Value = item.Value});
            }
         }

         foreach (InputTable item in filterInputs)
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

               // remove from the utxo table
               if (!utxoToAdd.Remove(item.Outpoint.ToString()))
               {
                  // if not found in memory we need to delete form disk
                  utxoToDelete.Add(item.Outpoint.ToString(), item.Outpoint);
               }
            }
         }

         if (transcations.Any())
         {
            foreach ((string key, MapAddressBag mapAddressBag) in transcations.OrderBy(o => o.Value.BlockIndex))
            {
               var historyItem = new AddressHistoryComputedTable
               {
                  Address = addressComputedTable.Address,
                  TransactionId = key,
                  BlockIndex = Convert.ToUInt32(mapAddressBag.BlockIndex),
                  Id = $"{key}-{address}",
               };

               history.Add(key, historyItem);

               foreach (OutputTable output in mapAddressBag.Ouputs)
                  historyItem.AmountInOutputs += output.Value;

               foreach (InputTable output in mapAddressBag.Inputs)
                  historyItem.AmountInInputs += output.Value;

               if (mapAddressBag.CoinBase)
               {
                  countMined++;
                  mined += historyItem.AmountInOutputs;
                  historyItem.EntryType = "mine";
               }
               else if (mapAddressBag.CoinStake)
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
            long position = addressComputedTable.CountSent + addressComputedTable.CountReceived + addressComputedTable.CountStaked + addressComputedTable.CountMined;
            foreach (AddressHistoryComputedTable historyValue in history.Values.OrderBy(o => o.BlockIndex))
            {
               historyValue.Position = ++position;
            }

            addressComputedTable.Received += received;
            addressComputedTable.Staked += staked;
            addressComputedTable.Mined += mined;
            addressComputedTable.Sent += sent;
            addressComputedTable.Available = addressComputedTable.Received + addressComputedTable.Mined + addressComputedTable.Staked - addressComputedTable.Sent;
            addressComputedTable.CountReceived += countReceived;
            addressComputedTable.CountSent += countSent;
            addressComputedTable.CountStaked += countStaked;
            addressComputedTable.CountMined += countMined;
            addressComputedTable.CountUtxo = addressComputedTable.CountUtxo - utxoToDelete.Count + utxoToAdd.Count;

            addressComputedTable.ComputedBlockIndex = maxHeight; // the last block a trx was received to this address

            if (addressComputedTable.Available < 0)
            {
               throw new ApplicationException("Failed to compute balance correctly");
            }

            try
            {
               // only push to store if the same version of computed bloc index is present (meaning entry was not modified)
               // block height must change if new trx are added so use it to apply OCC (Optimistic Concurrency Control)
               // to determine if a newer entry was pushed to store.
               FilterDefinition<AddressComputedTable> updateFilter = Builders<AddressComputedTable>.Filter
                  .Where(f => f.Address == address && f.ComputedBlockIndex == currentHeight);

               // update the computed address entry, this will throw if a newer version is in store
               mongoDb.AddressComputedTable.ReplaceOne(updateFilter, addressComputedTable, new ReplaceOptions { IsUpsert = true });
            }
            catch (MongoWriteException nwe)
            {
               if (nwe.WriteError.Category != ServerErrorCategory.DuplicateKey)
               {
                  throw;
               }

               // address was already modified fetch the latest version
               addressComputedTable = mongoDb.AddressComputedTable.Find(addrFilter).FirstOrDefault();

               return addressComputedTable;
            }

            var historyTask = Task.Run(() =>
            {
               try
               {
                  // if we managed to update the address we can safely insert history
                  mongoDb.AddressHistoryComputedTable.InsertMany(history.Values, new InsertManyOptions {IsOrdered = false});
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
            });

            Task.WaitAll(historyTask);
         }

         return addressComputedTable;
      }

      private class MapMempoolAddressBag
      {
         public long AmountInInputs;
         public long AmountInOutputs;
         public MempoolTable Mempool;
      }

      private class MapAddressBag
      {
         public long BlockIndex;
         public bool CoinBase;
         public bool CoinStake;

         public List<InputTable> Inputs = new();
         public List<OutputTable> Ouputs = new();
      }

      public async Task DeleteBlockAsync(string blockHash)
      {
          SyncBlockInfo block = BlockByHash(blockHash);

         if (!globalState.IndexModeCompleted)
         {
            log.LogWarning("Rewinding block without indexes this can be a long operation!");
         }

         await rewindOperation.RewindBlockAsync((uint)block.BlockIndex);

         // signal to any child classes to deleted a block.
         await OnDeleteBlockAsync(block);

         // delete the block itself is done last
         FilterDefinition<BlockTable> blockFilter = Builders<BlockTable>.Filter.Eq(info => info.BlockHash, blockHash);
         await mongoDb.BlockTable.DeleteOneAsync(blockFilter);
      }

      protected virtual async Task OnDeleteBlockAsync(SyncBlockInfo block)
      {
         await Task.CompletedTask;
      }

      public QueryResult<QueryMempoolTransactionHashes> GetMemoryTransactionsSlim(int offset, int limit)
      {
         ICollection<MempoolTable> list = mongoDb.Mempool.AsQueryable().OrderByDescending(o => o.FirstSeen).Skip(offset).Take(limit).ToList();

         var mempoolTransactions = new List<QueryMempoolTransactionHashes>();

         foreach (MempoolTable trx in list)
         {
            string transactionId = trx.TransactionId;

            mempoolTransactions.Add(new QueryMempoolTransactionHashes { TransactionId = transactionId });
         }

         var queryResult = new QueryResult<QueryMempoolTransactionHashes>
         {
            Items = mempoolTransactions,
            Total = mongoDb.Mempool.EstimatedDocumentCount(),
            Offset = offset,
            Limit = limit
         };

         return queryResult;
      }

      public QueryResult<QueryTransaction> GetMemoryTransactions(int offset, int limit)
      {
         ICollection<MempoolTable> list = mongoDb.Mempool.AsQueryable().Skip(offset).Take(limit).ToList();

         var retList = new List<QueryTransaction>();

         foreach (MempoolTable trx in list) // 1 based index, so we'll subtract one.
         {
            string transactionId = trx.TransactionId;
            SyncTransactionItems transactionItems = TransactionItemsGet(transactionId);

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
               Fee = transactionItems.Fee,
               Weight = transactionItems.Weight,
               Size = transactionItems.Size,
               VirtualSize = transactionItems.VirtualSize,
               HasWitness = transactionItems.HasWitness,
               Inputs = transactionItems.Inputs.Select(i => new QueryTransactionInput
               {
                  CoinBase = i.InputCoinBase,
                  InputAddress = i.InputAddress,
                  InputAmount = i.InputAmount,
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
            Total = mongoDb.Mempool.EstimatedDocumentCount(),
            Offset = offset,
            Limit = limit
         };

         return queryResult;
      }

      public int GetMemoryTransactionsCount()
      {
         return globalState.LocalMempoolView.Count;
      }

      public async Task<QueryResult<OutputTable>> GetUnspentTransactionsByAddressAsync(string address, long confirmations, int offset, int limit)
      {
         SyncBlockInfo storeTip = globalState.StoreTip;

         // TODO: This must be fixed, the tip will be null whenever the node is inaccessible.
         if (storeTip == null)
         {
            return null;
         }

         var totalTask = Task.Run(() => mongoDb.UnspentOutputTable.Aggregate()
            .Match(_ => _.Address.Equals(address))
            .Match(_ => _.BlockIndex <= storeTip.BlockIndex - confirmations)
            .Count()
            .SingleOrDefault());

         var outpointsToFetchTask = Task.Run(() => mongoDb.UnspentOutputTable.Aggregate()
            .Match(_ => _.Address.Equals(address))
            .Match(_ => _.BlockIndex <= storeTip.BlockIndex - confirmations)
            .Sort(Builders<UnspentOutputTable>.Sort.Ascending(x => x.BlockIndex).Ascending(x => x.Outpoint.OutputIndex))
            .Skip(offset)
            .Limit(limit)
            .ToList()
            .Select(_ => _.Outpoint));

         var mempoolBalanceTask = confirmations == 0 ?
            Task.Run(() => MempoolBalance(address)) :
            Task.FromResult<List<MapMempoolAddressBag>>(null);

         await Task.WhenAll(totalTask, outpointsToFetchTask, mempoolBalanceTask);

         var unspentOutputs = outpointsToFetchTask.Result.ToList();
         var mempoolItems = mempoolBalanceTask.Result;

         // remove any outputs that have been spent in the mempool
         mempoolItems?.ForEach(mp => mp.Mempool.Inputs.ForEach(input =>
         {
            if (input.Address == address)
            {
               Outpoint item = unspentOutputs.FirstOrDefault(w => w.ToString() == input.Outpoint.ToString());
               if (item != null)
                  unspentOutputs.Remove(item);
            }
         }));

         var results = await mongoDb.OutputTable.Aggregate()
            .Match(_ => unspentOutputs.Contains(_.Outpoint))
            .ToListAsync();

         // add any new unconfirmed outputs to the list
         mempoolItems?.ForEach(mp =>
         {
            int index = 0;
            foreach (MempoolOutput mempoolOutput in mp.Mempool.Outputs)
            {
               if (mempoolOutput.Address == address)
               {
                  results.Add(new OutputTable
                  {
                     Address = address,
                     BlockIndex = 0,
                     ScriptHex = mempoolOutput.ScriptHex,
                     Value = mempoolOutput.Value,
                     Outpoint = new Outpoint { TransactionId = mp.Mempool.TransactionId, OutputIndex = index }
                  });
               }

               index++;
            }
         });

         return new QueryResult<OutputTable>
         {
            Items = results.OrderBy(o => o.BlockIndex),
            Total = totalTask.Result?.Count ?? 0,
            Offset = offset,
            Limit = limit
         };
      }
   }
}
