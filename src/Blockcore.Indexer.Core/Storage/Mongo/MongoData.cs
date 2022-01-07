using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class MongoData : MongoDb, IStorage
   {
      readonly IScriptInterpeter scriptInterpeter;

      private readonly IMapMongoBlockToStorageBlock mongoBlockToStorageBlock;
      readonly ICryptoClientFactory clientFactory;
      public MongoData(ILogger<MongoDb> dbLogger, SyncConnection connection, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainConfiguration, GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock, ICryptoClientFactory clientFactory,IScriptInterpeter scriptInterpeter)
         : base(dbLogger,  connection, nakoConfiguration, chainConfiguration, globalState)
      {
         this.mongoBlockToStorageBlock = mongoBlockToStorageBlock;
         this.clientFactory = clientFactory;
         this.scriptInterpeter = scriptInterpeter;
      }

      public List<IndexView> GetCurrentIndexes()
      {
            IMongoDatabase db = mongoClient.GetDatabase("admin");
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

            RBF = transactionItems.RBF,
            LockTime = transactionItems.LockTime.ToString(),
            Version = transactionItems.Version,
            IsCoinbase = transactionItems.IsCoinbase,
            IsCoinstake = transactionItems.IsCoinstake,

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
      public QueryResult<SyncBlockInfo> Blocks(int offset, int limit)
      {
         // page using the block height as paging counter
         SyncBlockInfo storeTip = globalState.StoreTip;
         long total = storeTip?.BlockIndex ?? BlockTable.Find(Builders<BlockTable>.Filter.Empty).CountDocuments() - 1;

         if (total == -1) total = 0;

         if (offset == 0 || offset > total)
            offset = (int)total;

         IQueryable<BlockTable> filter = BlockTable.AsQueryable().Where(w => w.BlockIndex <= offset && w.BlockIndex > offset - limit);
         IEnumerable<SyncBlockInfo> list = filter.ToList().Select(mongoBlockToStorageBlock.Map);

         return new QueryResult<SyncBlockInfo> { Items = list, Total = total, Offset = offset, Limit = limit };
      }

      public SyncBlockInfo BlockByIndex(long blockIndex)
      {
         FilterDefinition<BlockTable> filter = Builders<BlockTable>.Filter.Eq(info => info.BlockIndex, blockIndex);

         return BlockTable.Find(filter).ToList().Select(mongoBlockToStorageBlock.Map).FirstOrDefault();
      }

      public SyncBlockInfo BlockByHash(string blockHash)
      {
         FilterDefinition<BlockTable> filter = Builders<BlockTable>.Filter.Eq(info => info.BlockHash, blockHash);

         return BlockTable.Find(filter).ToList().Select(mongoBlockToStorageBlock.Map).FirstOrDefault();
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
         FilterDefinition<TransactionTable> filter = Builders<TransactionTable>.Filter.Eq(info => info.TransactionId, trxHash);

         return TransactionTable.Find(filter).ToList().Select(t => new SyncRawTransaction { TransactionHash = trxHash, RawTransaction = t.RawTransaction }).FirstOrDefault();
      }

      public InputTable GetTransactionInput(string transaction, int index)
      {
         FilterDefinition<InputTable> filter = Builders<InputTable>.Filter.Eq(addr => addr.Outpoint, new Outpoint { TransactionId = transaction, OutputIndex = index });

         return InputTable.Find(filter).ToList().FirstOrDefault();
      }

      public OutputTable GetTransactionOutput(string transaction, int index)
      {
         FilterDefinition<OutputTable> filter = Builders<OutputTable>.Filter.Eq(addr => addr.Outpoint, new Outpoint { TransactionId = transaction, OutputIndex = index });

         return OutputTable.Find(filter).ToList().FirstOrDefault();
      }

      public SyncTransactionInfo BlockTransactionGet(string transactionId)
      {
         FilterDefinition<TransactionBlockTable> filter = Builders<TransactionBlockTable>.Filter.Eq(info => info.TransactionId, transactionId);

         TransactionBlockTable trx = TransactionBlockTable.Find(filter).FirstOrDefault();
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

         return ret;
      }

      public QueryResult<RichlistTable> Richlist(int offset, int limit)
      {
         FilterDefinitionBuilder<RichlistTable> filterBuilder = Builders<RichlistTable>.Filter;
         FilterDefinition<RichlistTable> filter = filterBuilder.Empty;

         // Skip and Limit only supports int, so we can't support long amount of documents.
         int total = (int)RichlistTable.Find(filter).CountDocuments();

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

         IEnumerable<RichlistTable> list = RichlistTable.Find(filter)
                   .SortBy(p => p.Balance)
                   .Skip(offset - 1) // 1 based index, so we'll subtract one.
                   .Limit(limit)
                   .ToList();

         return new QueryResult<RichlistTable> { Items = list, Total = total, Offset = offset, Limit = limit };
      }

      public RichlistTable RichlistBalance(string address)
      {
         FilterDefinitionBuilder<RichlistTable> filterBuilder = Builders<RichlistTable>.Filter;
         FilterDefinition<RichlistTable> filter = filterBuilder.Eq(m => m.Address, address);

         RichlistTable table = RichlistTable.Find(filter).SingleOrDefault();

         return table;
      }

      public List<RichlistTable> AddressBalances(IEnumerable<string> addresses)
      {
         FilterDefinitionBuilder<RichlistTable> filterBuilder = Builders<RichlistTable>.Filter;
         FilterDefinition<RichlistTable> filter = filterBuilder.Where(s => addresses.Contains(s.Address));

         List<RichlistTable> document = RichlistTable.Find(filter).ToList();

         return document;
      }

      public long TotalBalance()
      {
         FilterDefinitionBuilder<RichlistTable> builder = Builders<RichlistTable>.Filter;
         IQueryable<RichlistTable> filter = RichlistTable.AsQueryable();

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
         FilterDefinition<TransactionBlockTable> filter = Builders<TransactionBlockTable>.Filter.Eq(info => info.BlockIndex, index);

         int total = (int)TransactionBlockTable.Find(filter).CountDocuments();

         // Can we do sorting?
         // SortDefinition<BlockTable> sort = Builders<BlockTable>.Sort.Descending(info => info.BlockIndex);

         IEnumerable<SyncTransactionInfo> list = TransactionBlockTable.Find(filter)
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
         AddressComputedTable addressComputedTable = ComputeAddressBalance(address);


         IQueryable<AddressHistoryComputedTable> filter = AddressHistoryComputedTable.AsQueryable()
            .Where(t => t.Address == address);

         SyncBlockInfo storeTip = globalState.StoreTip;
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
         long total = addressComputedTable.CountSent + addressComputedTable.CountReceived + addressComputedTable.CountStaked + addressComputedTable.CountMined;

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
            Confirmations = globalState.StoreTip.BlockIndex + 1 - item.BlockIndex
         });

         if (offset == total)
         {
            // TODO: add mempool in to history only when the page is the tip (offset = 1 or total) with zero confirmations
            // List<MapMempoolAddressBag> mempoolAddressBag = MempoolBalance(address);
         }

         return new QueryResult<QueryAddressItem>
         {
            Items = transactions,
            Offset = offset,
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
            TotalMine = addressComputedTable.CountMined,
            TotalSent = addressComputedTable.Sent,
            TotalReceivedCount = addressComputedTable.CountReceived,
            TotalSentCount = addressComputedTable.CountSent,
            TotalStakeCount = addressComputedTable.CountStaked,
            TotalMineCount = addressComputedTable.CountMined,
            PendingSent = mempoolAddressBag.Sum(s => s.AmountInInputs),
            PendingReceived = mempoolAddressBag.Sum(s => s.AmountInOutputs)
         };
      }

      private List<MapMempoolAddressBag> MempoolBalance(string address)
      {
         var mapMempoolAddressBag = new List<MapMempoolAddressBag>();

         if (globalState.LocalMempoolView.IsEmpty)
            return mapMempoolAddressBag;

         IQueryable<MempoolTable> mempoolForAddress = Mempool.AsQueryable()
            .Where(m => m.AddressInputs.Contains(address) || m.AddressOutputs.Contains(address));

         foreach (MempoolTable mempool in mempoolForAddress)
         {
            var bag = new MapMempoolAddressBag();
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
         if (globalState.IndexModeCompleted == false)
         {
            // do not compute tables if indexes have not run.
            throw new ApplicationException("node in syncing process");
         }

         FilterDefinition<AddressComputedTable> addrFilter = Builders<AddressComputedTable>.Filter
            .Where(f => f.Address == address);
         AddressComputedTable addressComputedTable = AddressComputedTable.Find(addrFilter).FirstOrDefault();

         if (addressComputedTable == null)
         {
            addressComputedTable = new AddressComputedTable() { Id = address, Address = address, ComputedBlockIndex = 0 };
            AddressComputedTable.ReplaceOne(addrFilter, addressComputedTable, new ReplaceOptions { IsUpsert = true });
         }

         SyncBlockInfo storeTip = globalState.StoreTip;
         if (storeTip == null)
            return addressComputedTable; // this can happen if node is in the middle of reorg

         long currentHeight = addressComputedTable.ComputedBlockIndex;
         long tipHeight = storeTip.BlockIndex;

         IQueryable<OutputTable> filterOutputs = OutputTable.AsQueryable()
            .Where(t => t.Address == address)
            .Where(b => b.BlockIndex > currentHeight && b.BlockIndex <= tipHeight);

         IQueryable<InputTable> filterInputs = InputTable.AsQueryable()
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
            foreach (KeyValuePair<string, MapAddressBag> item in transcations.OrderBy(o => o.Value.BlockIndex))
            {
               var historyItem = new AddressHistoryComputedTable
               {
                  Address = addressComputedTable.Address,
                  TransactionId = item.Key,
                  BlockIndex = item.Value.BlockIndex,
                  Id = $"{item.Key}-{address}",
               };

               history.Add(item.Key, historyItem);

               foreach (OutputTable output in item.Value.Ouputs)
                  historyItem.AmountInOutputs += output.Value;

               foreach (InputTable output in item.Value.Inputs)
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
               AddressComputedTable.ReplaceOne(updateFilter, addressComputedTable, new ReplaceOptions { IsUpsert = true });
            }
            catch (MongoWriteException nwe)
            {
               if (nwe.WriteError.Category != ServerErrorCategory.DuplicateKey)
               {
                  throw;
               }

               // address was already modified fetch the latest version
               addressComputedTable = AddressComputedTable.Find(addrFilter).FirstOrDefault();

               return addressComputedTable;
            }

            var historyTask = Task.Run(() =>
            {
               try
               {
                  // if we managed to update the address we can safely insert history
                  AddressHistoryComputedTable.InsertMany(history.Values, new InsertManyOptions {IsOrdered = false});
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

            var utxoTask = Task.Run(() =>
            {
               try
               {
                  var utxoEntriesToModify = new Dictionary<string, WriteModel<AddressUtxoComputedTable>>();

                  foreach (KeyValuePair<string, AddressUtxoComputedTable> utxoComputedTable in utxoToAdd)
                  {
                     utxoEntriesToModify.Add(utxoComputedTable.Key, new InsertOneModel<AddressUtxoComputedTable>(utxoComputedTable.Value));
                  }

                  foreach (KeyValuePair<string, Outpoint> toDelete in utxoToDelete)
                  {
                     FilterDefinition<AddressUtxoComputedTable> filter = Builders<AddressUtxoComputedTable>.Filter.Eq(entry => entry.Outpoint, toDelete.Value);
                     utxoEntriesToModify.Add(toDelete.Key, new DeleteOneModel<AddressUtxoComputedTable>(filter));
                  }

                  if (utxoEntriesToModify.Values.Any())
                  {
                     // if we managed to update the address we can safely insert history
                     AddressUtxoComputedTable.BulkWrite(utxoEntriesToModify.Values, new BulkWriteOptions {IsOrdered = false});
                  }
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

            Task.WaitAll(historyTask, utxoTask);
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

         public List<InputTable> Inputs = new List<InputTable>();
         public List<OutputTable> Ouputs = new List<OutputTable>();
      }

      public async Task DeleteBlockAsync(string blockHash)
      {
         SyncBlockInfo block = BlockByHash(blockHash);

         FilterDefinition<InputTable> addrForInputFilter = Builders<InputTable>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> input = InputTable.DeleteManyAsync(addrForInputFilter);

         FilterDefinition<OutputTable> addrForOutputFilter = Builders<OutputTable>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> output = OutputTable.DeleteManyAsync(addrForOutputFilter);

         // delete the transaction
         FilterDefinition<TransactionBlockTable> transactionFilter = Builders<TransactionBlockTable>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         Task<DeleteResult> transactions = TransactionBlockTable.DeleteManyAsync(transactionFilter);

         // delete computed
         FilterDefinition<AddressComputedTable> addrCompFilter = Builders<AddressComputedTable>.Filter.Eq(addr => addr.ComputedBlockIndex, block.BlockIndex);
         Task<DeleteResult> addressComputed = AddressComputedTable.DeleteManyAsync(addrCompFilter);

         // delete computed history
         FilterDefinition<AddressHistoryComputedTable> addrCompHistFilter = Builders<AddressHistoryComputedTable>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> addressHistoryComputed = AddressHistoryComputedTable.DeleteManyAsync(addrCompHistFilter);

         // delete computed utxo
         FilterDefinition<AddressUtxoComputedTable> addrCompUtxoFilter = Builders<AddressUtxoComputedTable>.Filter.Eq(addr => addr.BlockIndex, block.BlockIndex);
         Task<DeleteResult> addressUtxoComputed = AddressUtxoComputedTable.DeleteManyAsync(addrCompUtxoFilter);

         await Task.WhenAll(input, output, transactions, addressComputed, addressHistoryComputed, addressUtxoComputed);

         // delete the block itself is done last
         FilterDefinition<BlockTable> blockFilter = Builders<BlockTable>.Filter.Eq(info => info.BlockHash, blockHash);
         await BlockTable.DeleteOneAsync(blockFilter);
      }

      public QueryResult<QueryTransaction> GetMemoryTransactions(int offset, int limit)
      {
         ICollection<MempoolTable> list = Mempool.AsQueryable().Skip(offset).Take(limit).ToList();

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
            Total = list.Count,
            Offset = offset,
            Limit = limit
         };

         return queryResult;
      }

      public int GetMemoryTransactionsCount()
      {
         return globalState.LocalMempoolView.Count;
         //return (int)Mempool.CountDocuments(FilterDefinition<Mempool>.Empty);
      }

      public async Task<QueryResult<UnspentOutputsView>> GetUnspentTransactionsByAddressAsync(string address ,long confirmations, int offset, int limit)
      {
         // make sure fields are computed
         AddressComputedTable addressComputedTable = ComputeAddressBalance(address);

         long total = addressComputedTable.CountUtxo;

         IEnumerable<UnspentOutputsView> utxos = AddressUtxoComputedTable.AsQueryable()
            .Where(utxo => utxo.Address == address)
            .Skip(offset)
            .Take(limit)
            .ToList()
            .Select(utxo => new UnspentOutputsView
            {
               Address = utxo.Address,
               Outpoint = utxo.Outpoint,
               Value = utxo.Value,
               BlockIndex = utxo.BlockIndex,
               CoinBase = utxo.CoinBase,
               CoinStake = utxo.CoinStake,
               ScriptHex = utxo.ScriptHex
            });

         return new QueryResult<UnspentOutputsView>
         {
            Items = utxos,
            Total = total,
            Offset = offset,
            Limit = limit
         };

      }

      public async Task<QueryResult<UnspentOutputsView>> GetUnspentTransactionsByAddressAsync_Old(string address, long confirmations, int offset, int limit)
      {
         var totalTask = Task.Run(() => OutputTable.Aggregate()
            .Match(_ => _.Address.Equals(address))
            .Match(_ => _.BlockIndex <= globalState.StoreTip.BlockIndex - confirmations)
            .Lookup(InputTable.CollectionNamespace.CollectionName,
               new StringFieldDefinition<OutputTable>(nameof(Outpoint)),
               new StringFieldDefinition<BsonDocument>(nameof(Outpoint)),
               new StringFieldDefinition<BsonDocument>("Inputs"))
            .Match(_ => _["Inputs"] == new BsonArray())
            .Count()
            .Single());

         var selectedTask = Task.Run(() => OutputTable.Aggregate()
            .Match(_ => _.Address.Equals(address))
            .Match(_ => _.BlockIndex <= globalState.StoreTip.BlockIndex - confirmations)
            .Sort(new BsonDocumentSortDefinition<OutputTable>(new BsonDocument("BlockIndex",-1)))
            .Lookup(InputTable.CollectionNamespace.CollectionName,
               new StringFieldDefinition<OutputTable>(nameof(Outpoint)),
               new StringFieldDefinition<BsonDocument>(nameof(Outpoint)),
               new StringFieldDefinition<BsonDocument>("Inputs"))
            .Match(_ => _["Inputs"] == new BsonArray())
            .Skip(offset * limit)
            .Limit(limit)
            .ToList()
            .Select(_ => new UnspentOutputsView
            {
               Address = _["Address"].AsString,
               Outpoint = new Outpoint
               {
                  OutputIndex = _["Outpoint"]["OutputIndex"].AsInt32,
                  TransactionId = _["Outpoint"]["TransactionId"].AsString,
               }  ,
               Value = _["Value"].AsInt64,
               BlockIndex = _["BlockIndex"].AsInt64,
               CoinBase = _["CoinBase"].AsBoolean,
               CoinStake = _["CoinStake"].AsBoolean,
               ScriptHex = _["ScriptHex"].AsString
            }));

         await Task.WhenAll(totalTask, selectedTask);

         return new QueryResult<UnspentOutputsView>
         {
            Items = selectedTask.Result,
            Total = totalTask.Result.Count,
            Offset = offset,
            Limit = limit
         };
      }
   }
}
