using Blockcore.Indexer.Crypto;

namespace Blockcore.Indexer.Storage.Mongo
{
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using Blockcore.Indexer.Client.Types;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Operations;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage.Mongo.Types;
   using Blockcore.Indexer.Storage.Types;
   using Blockcore.Indexer.Sync;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.Options;
   using MongoDB.Driver;
   using NBitcoin;
   using Blockcore.Consensus.TransactionInfo;

   /// <summary>
   /// Mongo storage operations.
   /// </summary>
   public class MongoStorageOperations : IStorageOperations
   {
      private readonly IStorage storage;

      private readonly ILogger<MongoStorageOperations> log;
      private readonly SyncConnection syncConnection;

      private readonly IndexerSettings configuration;

      private readonly MongoData data;

      /// <summary>
      /// Initializes a new instance of the <see cref="MongoStorageOperations"/> class.
      /// </summary>
      public MongoStorageOperations(IStorage storage, ILogger<MongoStorageOperations> logger, IOptions<IndexerSettings> configuration, SyncConnection syncConnection)
      {
         data = (MongoData)storage;
         this.configuration = configuration.Value;
         log = logger;
         this.syncConnection = syncConnection;
         this.storage = storage;
      }

      public void ValidateBlock(SyncBlockTransactionsOperation item)
      {
         if (item.BlockInfo != null)
         {
            SyncBlockInfo lastBlock = storage.GetLatestBlock();

            if (lastBlock != null)
            {
               if (lastBlock.BlockHash == item.BlockInfo.Hash)
               {
                  if (lastBlock.SyncComplete)
                  {
                     throw new InvalidOperationException("This should never happen.");
                  }
               }
               else
               {
                  if (item.BlockInfo.PreviousBlockHash != lastBlock.BlockHash)
                  {
                     InvalidBlockFound(lastBlock, item);
                     return;
                  }

                  CreateBlock(item.BlockInfo);

                  ////if (string.IsNullOrEmpty(lastBlock.NextBlockHash))
                  ////{
                  ////    lastBlock.NextBlockHash = item.BlockInfo.Hash;
                  ////    this.SyncOperations.UpdateBlockHash(lastBlock);
                  ////}
               }
            }
            else
            {
               CreateBlock(item.BlockInfo);
            }
         }
      }

      public InsertStats InsertTransactions(SyncBlockTransactionsOperation item)
      {
         var stats = new InsertStats { Items = new List<MapTransactionAddress>() };

         if (item.BlockInfo != null)
         {
            // remove all transactions from the memory pool
            item.Transactions.ForEach(t =>
                {
                   data.MemoryTransactions.TryRemove(t.GetHash().ToString(), out Transaction outer);
                });

            // break the work in to batches of transactions
            var queue = new Queue<Transaction>(item.Transactions);
            do
            {
               var items = GetBatch(configuration.MongoBatchSize, queue).ToList();

               try
               {
                  if (item.BlockInfo != null)
                  {
                     var inserts = items.Select(s => new MapTransactionBlock { BlockIndex = item.BlockInfo.Height, TransactionId = s.GetHash().ToString() }).ToList();
                     stats.Transactions += inserts.Count;
                     data.MapTransactionBlock.InsertMany(inserts, new InsertManyOptions { IsOrdered = false });
                  }
               }
               catch (MongoBulkWriteException mbwex)
               {
                  if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))//.Message.Contains("E11000 duplicate key error collection"))
                  {
                     throw;
                  }
               }

               // insert inputs and add to the list for later to use on the notification task.
               var inputs = CreateInputs(item.BlockInfo.Height, items).ToList();
               // inputs.ForEach(i => data.AddBalanceRichlist(i));
               var outputs = CreateOutputs(items, item.BlockInfo.Height).ToList();
               inputs.AddRange(outputs);
               var queueInner = new Queue<MapTransactionAddress>(inputs);

               do
               {
                  try
                  {
                     var itemsInner = GetBatch(configuration.MongoBatchSize, queueInner).ToList();
                     var ops = new Dictionary<string, WriteModel<MapTransactionAddress>>();
                     var writeOptions = new BulkWriteOptions() { IsOrdered = false };

                     foreach (MapTransactionAddress mapTransactionAddress in itemsInner)
                     {
                        if (mapTransactionAddress.SpendingTransactionId == null)
                        {
                           ops.Add(mapTransactionAddress.Id, new InsertOneModel<MapTransactionAddress>(mapTransactionAddress));
                        }
                        else
                        {
                           if (ops.TryGetValue(mapTransactionAddress.Id, out WriteModel<MapTransactionAddress> mta))
                           {
                              // in case a utxo is spent in the same block
                              // we just modify the inserted item directly

                              var imta = mta as InsertOneModel<MapTransactionAddress>;
                              imta.Document.SpendingTransactionId = mapTransactionAddress.SpendingTransactionId;
                              imta.Document.SpendingBlockIndex = mapTransactionAddress.SpendingBlockIndex;
                           }
                           else
                           {
                              FilterDefinition<MapTransactionAddress> filter = Builders<MapTransactionAddress>.Filter.Eq(addr => addr.Id, mapTransactionAddress.Id);

                              UpdateDefinition<MapTransactionAddress> update = Builders<MapTransactionAddress>.Update
                                  .Set(blockInfo => blockInfo.SpendingTransactionId, mapTransactionAddress.SpendingTransactionId)
                                  .Set(blockInfo => blockInfo.SpendingBlockIndex, mapTransactionAddress.SpendingBlockIndex);

                              ops.Add(mapTransactionAddress.Id, new UpdateOneModel<MapTransactionAddress>(filter, update));
                           }
                        }
                     }

                     if (itemsInner.Any())
                     {
                        stats.Items.AddRange(itemsInner);
                        stats.InputsOutputs += ops.Count;
                        data.MapTransactionAddress.BulkWrite(ops.Values, writeOptions);
                     }
                  }
                  catch (MongoBulkWriteException mbwex)
                  {
                     if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))//.Message.Contains("E11000 duplicate key error collection"))
                     {
                        throw;
                     }
                  }
               }
               while (queueInner.Any());

               // If insert trx supported then push trx in batches.
               if (configuration.StoreRawTransactions)
               {
                  try
                  {
                     var inserts = items.Select(t => new MapTransaction { TransactionId = t.GetHash().ToString(), RawTransaction = t.ToBytes(syncConnection.Network.Consensus.ConsensusFactory) }).ToList();
                     stats.RawTransactions = inserts.Count;
                     data.MapTransaction.InsertMany(inserts, new InsertManyOptions { IsOrdered = false });
                  }
                  catch (MongoBulkWriteException mbwex)
                  {
                     if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))//.Message.Contains("E11000 duplicate key error collection"))
                     {
                        throw;
                     }
                  }
               }
            }
            while (queue.Any());

            // mark the block as synced.
            CompleteBlock(item.BlockInfo);

            // Adds data to richlist
            //IEnumerable<MapTransactionAddress> spent = stats.Items.Where(i => i.SpendingTransactionId != null);
            //foreach (MapTransactionAddress trans in spent)
            //{
            //   if (trans.Addresses == null)
            //   {
            //      data.RemoveBalanceRichlist(trans);
            //   }
            //}
         }
         else
         {
            // memory transaction push in to the pool.
            item.Transactions.ForEach(t =>
            {
               data.MemoryTransactions.TryAdd(t.GetHash().ToString(), t);
            });

            stats.Transactions = data.MemoryTransactions.Count();

            // todo: for accuracy - remove transactions from the mongo memory pool that are not anymore in the syncing pool
            // remove all transactions from the memory pool
            // this can be done using the SyncingBlocks objects - see method SyncOperations.FindPoolInternal()

            // add to the list for later to use on the notification task.
            var inputs = CreateInputs(-1, item.Transactions).ToList();
            stats.Items.AddRange(inputs);
         }

         return stats;
      }

      public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         storageBatch.MapBlocks.Add(CreateMapBlock(item.BlockInfo));
         storageBatch.TotalSize += item.BlockInfo.Size;

         storageBatch.MapTransactionBlocks.AddRange(item.Transactions.Select(s => new MapTransactionBlock
         {
            BlockIndex = item.BlockInfo.Height,
            TransactionId = s.GetHash().ToString()
         }));

         IEnumerable<MapTransactionAddress> inputs = CreateInputs(item.BlockInfo.Height, item.Transactions);
         foreach (MapTransactionAddress mapTransactionAddress in inputs)
         {
            storageBatch.MapTransactionAddresses.Add(mapTransactionAddress.Id, new InsertOneModel<MapTransactionAddress>(mapTransactionAddress));
         }

         IEnumerable<MapTransactionAddress> outputs = CreateOutputs(item.Transactions, item.BlockInfo.Height);
         foreach (MapTransactionAddress mapTransactionAddress in outputs)
         {
            if (storageBatch.MapTransactionAddresses.TryGetValue(mapTransactionAddress.Id, out WriteModel<MapTransactionAddress> MapTransactionAddressOut))
            {
               // in case a utxo is spent in the same block we just modify the inserted item directly

               var imta = MapTransactionAddressOut as InsertOneModel<MapTransactionAddress>;
               imta.Document.SpendingTransactionId = mapTransactionAddress.SpendingTransactionId;
               imta.Document.SpendingBlockIndex = mapTransactionAddress.SpendingBlockIndex;
            }
            else
            {
               FilterDefinition<MapTransactionAddress> filter = Builders<MapTransactionAddress>.Filter.Eq(addr => addr.Id, mapTransactionAddress.Id);

               UpdateDefinition<MapTransactionAddress> update = Builders<MapTransactionAddress>.Update
                   .Set(blockInfo => blockInfo.SpendingTransactionId, mapTransactionAddress.SpendingTransactionId)
                   .Set(blockInfo => blockInfo.SpendingBlockIndex, mapTransactionAddress.SpendingBlockIndex);

               storageBatch.MapTransactionAddresses.Add(mapTransactionAddress.Id, new UpdateOneModel<MapTransactionAddress>(filter, update));
            }
         }

         if (configuration.StoreRawTransactions)
         {
            storageBatch.MapTransactions.AddRange(item.Transactions.Select(t => new MapTransaction
            {
               TransactionId = t.GetHash().ToString(),
               RawTransaction = t.ToBytes(syncConnection.Network.Consensus.ConsensusFactory)
            }));
         }
      }

      public void PushStorageBatch(StorageBatch storageBatch)
      {
         data.MapBlock.InsertMany(storageBatch.MapBlocks, new InsertManyOptions { IsOrdered = false });
         data.MapTransactionBlock.InsertMany(storageBatch.MapTransactionBlocks, new InsertManyOptions { IsOrdered = false });
         data.MapTransactionAddress.BulkWrite(storageBatch.MapTransactionAddresses.Values, new BulkWriteOptions() { IsOrdered = false });

         if (storageBatch.MapTransactions.Any())
            data.MapTransaction.InsertMany(storageBatch.MapTransactions, new InsertManyOptions { IsOrdered = false });

         List<UpdateOneModel<MapBlock>> markBlocksAsComplete = new List<UpdateOneModel<MapBlock>>();
         foreach (MapBlock mapBlock in storageBatch.MapBlocks.OrderBy(b => b.BlockIndex))
         {
            FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(block => block.BlockIndex, mapBlock.BlockIndex);
            UpdateDefinition<MapBlock> update = Builders<MapBlock>.Update.Set(blockInfo => blockInfo.SyncComplete, true);

            markBlocksAsComplete.Add(new UpdateOneModel<MapBlock>(filter, update));
         }
         data.MapBlock.BulkWrite(markBlocksAsComplete, new BulkWriteOptions() { IsOrdered = true });

         storageBatch.TotalSize = 0;
         storageBatch.MapBlocks.Clear();
         storageBatch.MapTransactionBlocks.Clear();
         storageBatch.MapTransactionAddresses.Clear();
         storageBatch.MapTransactions.Clear();
      }

      private void CompleteBlock(BlockInfo block)
      {
         data.CompleteBlock(block.Hash);
      }

      private void CreateBlock(BlockInfo block)
      {
         MapBlock blockInfo = CreateMapBlock(block);

         data.InsertBlock(blockInfo);
      }

      private MapBlock CreateMapBlock(BlockInfo block)
      {
         return new MapBlock
         {
            BlockIndex = block.Height,
            BlockHash = block.Hash,
            BlockSize = block.Size,
            BlockTime = block.Time,
            NextBlockHash = block.NextBlockHash,
            PreviousBlockHash = block.PreviousBlockHash,
            TransactionCount = block.Transactions.Count(),
            Bits = block.Bits,
            Confirmations = block.Confirmations,
            Merkleroot = block.Merkleroot,
            Nonce = block.Nonce,
            ChainWork = block.ChainWork,
            Difficulty = block.Difficulty,
            PosBlockSignature = block.PosBlockSignature,
            PosBlockTrust = block.PosBlockTrust,
            PosChainTrust = block.PosChainTrust,
            PosFlags = block.PosFlags,
            PosHashProof = block.PosHashProof,
            PosModifierv2 = block.PosModifierv2,
            Version = block.Version,
            SyncComplete = false
         };
      }

      private IEnumerable<T> GetBatch<T>(int maxItems, Queue<T> queue)
      {
         //var total = 0;
         var items = new List<T>();

         // todo: optimize this
         var aggregate = Blockcore.Indexer.Extensions.Extensions.TakeAndRemove(queue, maxItems).ToList();
         items.AddRange(aggregate);

         //do
         //{
         //    var aggregate = Extensions.TakeAndRemove(queue, 100).ToList();

         //    items.AddRange(aggregate);

         //    total = items.SelectMany(s => s.VIn).Cast<object>().Concat(items.SelectMany(s => s.VOut).Cast<object>()).Count();
         //}
         //while (total < maxItems && queue.Any());

         return items;
      }

      private void InvalidBlockFound(SyncBlockInfo lastBlock, SyncBlockTransactionsOperation item)
      {
         // Re-org happened.
         throw new SyncRestartException();
      }

      private IEnumerable<SyncTransactionInfo> CreateTransactions(BlockInfo block, IEnumerable<DecodedRawTransaction> transactions)
      {
         IEnumerable<SyncTransactionInfo> trxInfps = transactions.Select(trx => new SyncTransactionInfo
         {
            TransactionHash = trx.TxId,
            Timestamp = block == null ? UnixUtils.DateToUnixTimestamp(DateTime.UtcNow) : block.Time
         });

         return trxInfps;
      }

      private IEnumerable<MapTransactionAddress> CreateInputs(long blockIndex, IEnumerable<Transaction> transactions)
      {
         foreach (Transaction transaction in transactions)
         {
            Transaction rawTransaction = transaction;

            string id = rawTransaction.GetHash().ToString();

            for (int index = 0; index < rawTransaction.Outputs.Count; index++)
            {
               TxOut output = rawTransaction.Outputs[index];

               string[] address = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey);

               if (address == null)
                  continue;

               yield return new MapTransactionAddress
               {
                  Id = string.Format("{0}-{1}", id, index),
                  TransactionId = id,
                  Value = output.Value,
                  Index = index,
                  Addresses = address.ToList(),
                  ScriptHex = output.ScriptPubKey.ToHex(),
                  BlockIndex = blockIndex,
                  CoinBase = rawTransaction.IsCoinBase,
                  CoinStake = syncConnection.Network.Consensus.IsProofOfStake && rawTransaction.IsCoinStake,
               };
            }
         }
      }

      private IEnumerable<MapTransactionAddress> CreateOutputs(IEnumerable<Transaction> transactions, long blockIndex)
      {
         foreach (Transaction transaction in transactions)
         {
            if (transaction.IsCoinBase)
               continue;

            foreach (TxIn input in transaction.Inputs)
            {
               yield return new MapTransactionAddress
               {
                  Id = string.Format("{0}-{1}", input.PrevOut.Hash, input.PrevOut.N),
                  SpendingTransactionId = transaction.GetHash().ToString(),
                  SpendingBlockIndex = blockIndex,
               };
            }
         }
      }
   }
}
