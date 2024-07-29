using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Extensions;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Sync;
using Blockcore.NBitcoin.DataEncoders;

// using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Input = Blockcore.Indexer.Core.Storage.Postgres.Types.Input;
using Output = Blockcore.Indexer.Core.Storage.Postgres.Types.Output;


namespace Blockcore.Indexer.Core.Storage.Postgres
{
    public class PostgresData : IStorage
    {
        private readonly ILogger<PostgresData> log;
        private readonly IDbContextFactory<PostgresDbContext> contextFactory;
        private readonly SyncConnection syncConnection;
        private readonly GlobalState globalState;
        private readonly ChainSettings chainConfiguration;

        private readonly IScriptInterpreter scriptInterpreter;

        private readonly IMapPgBlockToStorageBlock pgBlockToStorageBlock;
        readonly ICryptoClientFactory clientFactory;

        readonly IBlockRewindOperation rewindOperation;

        readonly IComputeHistoryQueue computeHistoryQueue;

        public PostgresData(ILogger<PostgresData> dbLogger, SyncConnection connection, IOptions<ChainSettings> chainConfiguration,
            GlobalState globalState, IMapPgBlockToStorageBlock pgBlockToStorageBlock, ICryptoClientFactory clientFactory,
            IScriptInterpreter scriptInterpreter, IDbContextFactory<PostgresDbContext> dbContextFactory, IBlockRewindOperation rewindOperation, IComputeHistoryQueue computeHistoryQueue)
        {
            log = dbLogger;
            this.chainConfiguration = chainConfiguration.Value;
            this.globalState = globalState;
            syncConnection = connection;

            this.pgBlockToStorageBlock = pgBlockToStorageBlock;
            this.clientFactory = clientFactory;
            this.scriptInterpreter = scriptInterpreter;
            contextFactory = dbContextFactory;
            this.rewindOperation = rewindOperation;
            this.computeHistoryQueue = computeHistoryQueue;
        }
        public List<string> GetBlockIndexIndexes()
        {
            PostgresDbContext db = contextFactory.CreateDbContext();

            List<string> indexNames = db.Database.SqlQuery<string>($"SELECT indexname FROM pg_indexes").ToList();
            return indexNames;
        }
        public bool DeleteTransactionsFromMempool(List<string> transactionIds)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();

            var result = db.mempoolTransactions.Where(txn => transactionIds.Contains(txn.TransactionId)).ExecuteDelete();
            return result == transactionIds.Count;
        }

        public List<string> GetMempoolTransactionIds()
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var res = db.mempoolTransactions.ToList().Select(t => t.TransactionId).ToList();
            return res;
        }

        public QueryTransaction GetTransaction(string transactionId)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            SyncTransactionInfo transaction = BlockTransactionGet(transactionId);
            SyncTransactionItems transactionItems = TransactionItemsGet(transactionId);

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
        public SyncBlockInfo GetLatestBlock()
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            if (globalState.StoreTip != null)
            {
                return globalState.StoreTip;
            }

            Block recentBlock = db.Blocks.OrderByDescending(a => a.BlockIndex).FirstOrDefault();

            if (recentBlock == null)
            {
                return null;
            }

            return pgBlockToStorageBlock.Map(recentBlock);
        }
        public QueryResult<SyncBlockInfo> Blocks(int? offset, int limit)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            SyncBlockInfo storeTip = globalState.StoreTip;
            long index = storeTip?.BlockIndex ?? db.Blocks.Count();

            // Get the total number of items based off the index.
            long total = index + 1;

            // If the offset has value, then use it, if not fetch the latest blocks.
            long startPosition = offset ?? total - limit;
            long endPosition = startPosition + limit;

            // The BlockIndex is 0 based, so we must perform >= to get first.
            var list = db.Blocks.OrderBy(b => b.BlockIndex).Where(b => b.BlockIndex >= startPosition && b.BlockIndex < endPosition)
                        .ToList()
                        .Select(pgBlockToStorageBlock.Map);
            return new QueryResult<SyncBlockInfo> { Items = list, Total = total, Offset = (int)startPosition, Limit = limit };
        }
        private SyncTransactionItems TransactionItemsGet(string transactionId, Consensus.TransactionInfo.Transaction transaction = null)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
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

            int size = NBitcoin.BitcoinSerializableExtensions.GetSerializedSize(transaction, syncConnection.Network.Consensus.ConsensusFactory);
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
                    InputAddress = scriptInterpreter.GetSignerAddress(syncConnection.Network, v.ScriptSig),
                    SequenceLock = v.Sequence.ToString(),
                }).ToList(),
                Outputs = transaction.Outputs.Select((output, index) => new SyncTransactionItemOutput
                {
                    Address = scriptInterpreter.InterpretScript(syncConnection.Network, output.ScriptPubKey)?.Addresses?.FirstOrDefault(),
                    Index = index,
                    Value = output.Value,
                    OutputType = scriptInterpreter.InterpretScript(syncConnection.Network, output.ScriptPubKey)?.ScriptType, // StandardScripts.GetTemplateFromScriptPubKey(output.ScriptPubKey)?.Type.ToString(),
                    ScriptPubKey = output.ScriptPubKey.ToHex()
                }).ToList()
            };

            foreach (SyncTransactionItemInput input in ret.Inputs)
            {
                Types.Output outputTable = GetTransactionOutput(input.PreviousTransactionHash, input.PreviousIndex);
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
        private Types.Input GetTransactionInput(string transactionId, int index)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var res = db.Inputs.Where(i => i.Outpoint == new Outpoint() { TransactionId = transactionId, OutputIndex = index }).FirstOrDefault();
            return res;
        }
        private Output GetTransactionOutput(string transactionId, int index)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var res = db.Outputs.Where(o => o.Outpoint == new Outpoint { OutputIndex = index, TransactionId = transactionId }).FirstOrDefault();
            return res;
        }
        private SyncRawTransaction TransactionGetByHash(string transactionId)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var res = db.Transactions.Find(transactionId);
            return new SyncRawTransaction { TransactionHash = transactionId, RawTransaction = res.RawTransaction };
        }
        private SyncTransactionInfo BlockTransactionGet(string transactionId)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            Transaction transaction = db.Transactions.Find(transactionId);

            if (transaction == null)
            {
                return null;
            }

            SyncBlockInfo current = globalState.StoreTip;
            SyncBlockInfo blk = BlockByIndex(transaction.BlockIndex);

            return new SyncTransactionInfo
            {
                BlockIndex = transaction.BlockIndex,
                BlockHash = blk.BlockHash,
                Timestamp = blk.BlockTime,
                TransactionHash = transaction.Txid,
                TransactionIndex = transaction.TransactionIndex,
                Confirmations = current.BlockIndex + 1 - transaction.BlockIndex
            };
        }
        public SyncBlockInfo BlockByIndex(long blockIndex)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            SyncBlockInfo block = pgBlockToStorageBlock.Map(db.Blocks.Find(blockIndex));

            SyncBlockInfo tip = globalState.StoreTip;

            if (tip != null && block != null)
            {
                block.Confirmations = tip.BlockIndex + 1 - block.BlockIndex;
            }

            return block;
        }
        public SyncBlockInfo BlockByHash(string blockHash)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            SyncBlockInfo block = pgBlockToStorageBlock.Map(db.Blocks.Where(b => b.BlockHash == blockHash).FirstOrDefault());
            SyncBlockInfo tip = globalState.StoreTip;

            if (tip != null && block != null)
            {
                block.Confirmations = tip.BlockIndex + 1 - block.BlockIndex;
            }

            return block; ;
        }
        public QueryResult<QueryOrphanBlock> OrphanBlocks(int? offset, int limit)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            int total = db.ReorgBlocks.Count();

            int itemsToSkip = offset ?? (total < limit ? 0 : total - limit);

            ICollection<ReorgBlock> list = db.ReorgBlocks
            .OrderBy(rb => rb.BlockIndex)
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
                    Block = MapQueryBlock(s.Block)
                }),
                Total = total,
                Offset = itemsToSkip,
                Limit = limit
            };
        }

        private QueryBlock MapQueryBlock(Block block)
        {
            return new QueryBlock()
            {
                BlockHash = block.BlockHash,
                BlockIndex = block.BlockIndex,
                BlockSize = block.BlockSize,
                BlockTime = block.BlockTime,
                NextBlockHash = block.NextBlockHash,
                PreviousBlockHash = block.PreviousBlockHash,
                Confirmations = block.Confirmations,
                Bits = block.Bits,
                Difficulty = block.Difficulty,
                ChainWork = block.ChainWork,
                Merkleroot = block.Merkleroot,
                Nonce = block.Nonce,
                Version = block.Version,
                Synced = block.SyncComplete,
                TransactionCount = block.TransactionCount,
                PosBlockSignature = block.PosBlockSignature,
                PosModifierv2 = block.PosModifierv2,
                PosFlags = block.PosFlags,
                PosHashProof = block.PosHashProof,
                PosBlockTrust = block.PosBlockTrust,
                PosChainTrust = block.PosChainTrust,
            };
        }
        public T OrphanBlockByHash<T>(string blockHash) where T : class => OrphanBlockByHash(blockHash) as T;

        public ReorgBlock OrphanBlockByHash(string blockHash)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            return db.ReorgBlocks.Where(rb => rb.BlockHash == blockHash).FirstOrDefault();
        }

        public async Task<long> InsertPeer(PeerDetails info)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            info.LastSeen = DateTime.UtcNow;

            db.Peers.Update(info);
            return await db.SaveChangesAsync().ContinueWith(p => (long)p.Result);
        }

        public List<PeerDetails> GetPeerFromDate(DateTime date)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var res = db.Peers.Where(p => p.LastSeen > date).ToList();
            return res;
        }

        public string GetRawTransaction(string transactionId)
        {
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

        //TODO - RichList
        public QueryResult<BalanceForAddress> Richlist(int offset, int limit)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var total = db.RichList.Count();

            if (offset == 0)
            {
                if (limit > total)
                {
                    offset = 0;
                }
                else
                {
                    offset = Math.Max(0, total - limit);
                }
            }
            else
            {
                offset -= 1;
            }

            var list = db.RichList.OrderByDescending(p => p.Balance)
                .Skip(offset)
                .Take(limit)
                .Select(x => new BalanceForAddress { Address = x.Address, Balance = x.Balance })
                .ToList();

            return new QueryResult<BalanceForAddress>
            {
                Items = list,
                Total = total,
                Offset = offset + 1,
                Limit = limit
            };
        }
        public List<BalanceForAddress> AddressBalances(IEnumerable<string> addresses)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var res = db.RichList.Where(x => addresses.Contains(x.Address))
            .ToList()
            .Select(x => new BalanceForAddress { Balance = x.Balance, Address = x.Address })
            .ToList();

            return res;
        }
        public QueryAddress AddressBalance(string address) => throw new NotImplementedException();
        public long TotalBalance()
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            return db.RichList.Sum(x => x.Balance);
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
            using PostgresDbContext db = contextFactory.CreateDbContext();
            int total = db.Transactions.Where(t => t.BlockIndex == index).Count();

            var list = db.Transactions.Where(t => t.BlockIndex == index)
            .OrderBy(p => p.TransactionIndex)
            .Skip(offset)
            .Take(limit)
            .Select(t => new SyncTransactionInfo
            {
                TransactionHash = t.Txid,
                TransactionIndex = t.TransactionIndex
            }).ToList();

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
            using PostgresDbContext db = contextFactory.CreateDbContext();
            AddressComputedEntry addressComputedTable = ComputeAddressBalance(address);

            IQueryable<AddressHistoryComputedEntry> addressComputedEntries = db.AddressHistoryComputedTable.AsQueryable()
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
            addressComputedEntries = addressComputedEntries.OrderBy(s => s.Position);

            long startPosition = offset ?? total - limit;
            long endPosition = (startPosition) + limit;

            // Get all items that is higher than start position and lower than end position.
            var list = addressComputedEntries.Where(w => w.Position > startPosition && w.Position <= endPosition).ToList();

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
        private AddressComputedEntry ComputeAddressBalance(string address)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            AddressComputedEntry addressComputedEntry = db.AddressComputedTable.Find(address);

            if (addressComputedEntry == null)
            {
                addressComputedEntry = new AddressComputedEntry() { Id = address, Address = address, ComputedBlockIndex = 0 };
                db.AddressComputedTable.Update(addressComputedEntry);
                db.SaveChanges();
            }

            SyncBlockInfo storeTip = globalState.StoreTip;
            if (storeTip == null)
            {
                return addressComputedEntry;
            }

            long currentHeight = addressComputedEntry.ComputedBlockIndex;
            long tipHeight = storeTip.BlockIndex;

            IQueryable<Output> outputs = db.Outputs.Where(t => t.Address == address && t.BlockIndex > currentHeight && t.BlockIndex <= tipHeight);
            IQueryable<Input> inputs = db.Inputs.Where(t => t.Address == address && t.BlockIndex > currentHeight && t.BlockIndex <= tipHeight);

            long countReceived = 0, countSent = 0, countStaked = 0, countMined = 0;
            long received = 0, sent = 0, staked = 0, mined = 0;
            long maxHeight = 0;

            var history = new Dictionary<string, AddressHistoryComputedEntry>();
            var transcations = new Dictionary<string, MapAddressBag>();
            var utxoToAdd = new Dictionary<string, AddressUtxoComputedEntry>();
            var utxoToDelete = new Dictionary<string, Outpoint>();


            //removed a redundant if statement
            foreach (Output item in outputs)
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
                    var bag = new MapAddressBag
                    {
                        BlockIndex = item.BlockIndex,
                        CoinBase = item.CoinBase,
                        CoinStake = item.CoinStake,
                    };
                    bag.Ouputs.Add(item);
                    transcations.Add(item.Outpoint.TransactionId, bag);
                }
                utxoToAdd.Add(item.Outpoint.ToString(), new AddressUtxoComputedEntry
                {
                    Outpoint = item.Outpoint,
                    BlockIndex = item.BlockIndex,
                    Address = item.Address,
                    CoinBase = item.CoinBase,
                    CoinStake = item.CoinStake,
                    ScriptHex = item.ScriptHex,
                    Value = item.Value
                });
            }

            foreach (Input item in inputs)
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
                if (!utxoToAdd.Remove(item.Outpoint.ToString()))
                {
                    // if not found in memory we need to delete form disk
                    utxoToDelete.Add(item.Outpoint.ToString(), item.Outpoint);
                }
            }

            if (transcations.Any())
            {
                foreach ((string key, MapAddressBag mapAddressBag) in transcations.OrderBy(o => o.Value.BlockIndex))
                {
                    var historyItem = new AddressHistoryComputedEntry
                    {
                        Address = addressComputedEntry.Address,
                        TransactionId = key,
                        BlockIndex = Convert.ToUInt32(mapAddressBag.BlockIndex),
                        _Id = $"{key}-{address}",
                    };

                    history.Add(key, historyItem);

                    foreach (Output output in mapAddressBag.Ouputs)
                        historyItem.AmountInOutputs += output.Value;

                    foreach (Input input in mapAddressBag.Inputs)
                        historyItem.AmountInInputs += input.Value;

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
                long position = addressComputedEntry.CountSent + addressComputedEntry.CountReceived + addressComputedEntry.CountStaked + addressComputedEntry.CountMined;
                foreach (AddressHistoryComputedEntry historyValue in history.Values.OrderBy(o => o.BlockIndex))
                {
                    historyValue.Position = ++position;
                }

                addressComputedEntry.Received += received;
                addressComputedEntry.Staked += staked;
                addressComputedEntry.Mined += mined;
                addressComputedEntry.Sent += sent;
                addressComputedEntry.Available = addressComputedEntry.Received + addressComputedEntry.Mined + addressComputedEntry.Staked - addressComputedEntry.Sent;
                addressComputedEntry.CountReceived += countReceived;
                addressComputedEntry.CountSent += countSent;
                addressComputedEntry.CountStaked += countStaked;
                addressComputedEntry.CountMined += countMined;
                addressComputedEntry.CountUtxo = addressComputedEntry.CountUtxo - utxoToDelete.Count + utxoToAdd.Count;

                addressComputedEntry.ComputedBlockIndex = maxHeight; // the last block a trx was received to this address

                if (addressComputedEntry.Available < 0)
                {
                    throw new ApplicationException("Failed to compute balance correctly");
                }

                try
                {
                    // only push to store if the same version of computed bloc index is present (meaning entry was not modified)
                    // block height must change if new trx are added so use it to apply OCC (Optimistic Concurrency Control)
                    // to determine if a newer entry was pushed to store.
                    db.AddressComputedTable.Update(addressComputedEntry);
                    db.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505") // 23505 is SQLSTATE code for unique violation
                    {
                        addressComputedEntry = db.AddressComputedTable.FirstOrDefault(ac => ac.Address == address && ac.ComputedBlockIndex == currentHeight);
                    }
                    else
                    {
                        throw; //rethrow the exception if it's not a unique violation
                    }

                    return addressComputedEntry;
                }

                var historyTask = Task.Run(() =>
                {
                    try
                    {
                        db.BulkInsert(history.Values, bulkConfig => bulkConfig.IncludeGraph = true); // . BulkInsert(history.Values, options => options.IncludeGraph = true);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        // Ignore unique constraint violations, as these indicate a duplicate key error
                        // which we're choosing to ignore based on the MongoDB logic provided
                    }
                    catch (Exception ex)
                    {
                        // Rethrow any other exceptions that we're not explicitly handling
                        throw;
                    }
                });

                Task.WaitAll(historyTask);
            }

            return addressComputedEntry;
        }

        public async Task<List<QueryAddressBalance>> QuickBalancesLookupForAddressesWithHistoryCheckAsync(IEnumerable<string> addresses, bool includePending = false)
        {

            using PostgresDbContext db = contextFactory.CreateDbContext();
            var outputTask = db.Outputs
            .Where(o => addresses.Contains(o.Address))
            .Select(o => o.Address)
            .Distinct()
            .ToListAsync();

            var utxoBalances = db.unspentOutputs
            .Where(o => addresses.Contains(o.Address))
            .GroupBy(o => o.Address)
            .Select(group => new
            {
                Address = group.Key,
                Balance = group.Sum(o => o.Value)
            })
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

        public async Task DeleteBlockAsync(string blockHash)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            SyncBlockInfo block = BlockByHash(blockHash);

            if (!globalState.IndexModeCompleted)
            {
                log.LogWarning("Rewinding block without indexes this can be a long operation");
            }
            await rewindOperation.RewindBlockAsync((uint)block.BlockIndex);

            await OnDeleteBlockAsync(block);

            await db.Blocks.Where(b => b.BlockHash == blockHash).ExecuteDeleteAsync();
        }

        protected virtual async Task OnDeleteBlockAsync(SyncBlockInfo block)
        {
            await Task.CompletedTask;
        }

        //   public List<IndexView> GetIndexesBuildProgress() => throw new NotImplementedException();
        public QueryResult<QueryTransaction> GetMemoryTransactions(int offset, int limit)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            ICollection<MempoolTransaction> list = db.mempoolTransactions.ToList().Skip(offset).Take(limit).ToList();

            var retList = new List<QueryTransaction>();

            foreach (MempoolTransaction trx in list)
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
                Total = db.mempoolTransactions.Count(),
                Offset = offset,
                Limit = limit
            };
            return queryResult;
        }
        public QueryResult<QueryMempoolTransactionHashes> GetMemoryTransactionsSlim(int offset, int limit)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            ICollection<MempoolTransaction> list = db.mempoolTransactions.OrderByDescending(o => o.FirstSeen).Skip(offset).Take(limit).ToList();

            var mempoolTransactions = new List<QueryMempoolTransactionHashes>();

            foreach (MempoolTransaction trx in list)
            {
                string transactionId = trx.TransactionId;

                mempoolTransactions.Add(new QueryMempoolTransactionHashes { TransactionId = transactionId });
            }

            var queryResult = new QueryResult<QueryMempoolTransactionHashes>
            {
                Items = mempoolTransactions,
                Total = db.mempoolTransactions.Count(),
                Offset = offset,
                Limit = limit
            };

            return queryResult;
        }
        public int GetMemoryTransactionsCount()
        {
            return globalState.LocalMempoolView.Count;
        }
        public async Task<QueryResult<Storage.Types.Output>> GetUnspentTransactionsByAddressAsync(string address, long confirmations, int offset, int limit)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            SyncBlockInfo storeTip = globalState.StoreTip;

            // TODO: This must be fixed, the tip will be null whenever the node is inaccessible.
            if (storeTip == null)
            {
                return null;
            }

            var totalTask = Task.Run(() => db.unspentOutputs
                                            .Where(uo => uo.Address.Equals(address) && uo.BlockIndex <= storeTip.BlockIndex - confirmations)
                                            .Count());


            var outpointsToFetchTask = Task.Run(() => db.unspentOutputs
                .Where(uo => uo.Address == address && uo.BlockIndex <= storeTip.BlockIndex - confirmations)
                .OrderBy(uo => uo.BlockIndex)
                .ThenBy(uo => uo.Outpoint.OutputIndex)
                .Skip(offset)
                .Take(limit)
                .Select(uo => uo.Outpoint)
                .ToList());


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

            var results = await db.Outputs.Where(o => unspentOutputs.Contains(o.Outpoint)).ToListAsync();

            // add any new unconfirmed outputs to the list
            mempoolItems?.ForEach(mp =>
            {
                int index = 0;
                foreach (MempoolOutput mempoolOutput in mp.Mempool.Outputs)
                {
                    if (mempoolOutput.Address == address)
                    {
                        results.Add(new Output
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

            return new QueryResult<Storage.Types.Output>
            {
                Items = results.OrderBy(o => o.BlockIndex),
                Total = totalTask.Result,
                Offset = offset,
                Limit = limit
            };
        }

        private List<MapMempoolAddressBag> MempoolBalance(string address)
        {
            using PostgresDbContext db = contextFactory.CreateDbContext();
            var mapMempoolAddressBag = new List<MapMempoolAddressBag>();

            if (globalState.LocalMempoolView.IsEmpty)
                return mapMempoolAddressBag;

            var mempoolForAddress = db.mempoolTransactions
                .Where(m => m.AddressInputs.Contains(address) || m.AddressOutputs.Contains(address))
                .ToList();

            foreach (MempoolTransaction mempool in mempoolForAddress)
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

        private class MapAddressBag
        {
            public long BlockIndex;
            public bool CoinBase;
            public bool CoinStake;

            public List<Input> Inputs = new();
            public List<Output> Ouputs = new();
        }
        private class MapMempoolAddressBag
        {
            public long AmountInInputs;
            public long AmountInOutputs;
            public MempoolTransaction Mempool;
        }
        private class MempoolAddressBag
        {
            public long AmountInInputs;
            public long AmountInOutputs;
            public MempoolTransaction Mempool;
        }
    }


}