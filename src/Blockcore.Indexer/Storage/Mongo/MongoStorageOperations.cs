using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Crypto;
using Blockcore.Indexer.Operations;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;
using Blockcore.Indexer.Sync.SyncTasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NBitcoin;

namespace Blockcore.Indexer.Storage.Mongo
{
   public class MongoStorageOperations : IStorageOperations
   {
      private readonly SyncConnection syncConnection;
      readonly SyncingBlocks syncingBlocks;
      readonly IndexerSettings configuration;
      private readonly MongoData data;

      public MongoStorageOperations(
         SyncConnection syncConnection,
         IStorage storage,
         IUtxoCache utxoCache,
         IOptions<IndexerSettings> configuration,
         SyncingBlocks syncingBlocks)
      {
         this.syncConnection = syncConnection;
         this.syncingBlocks = syncingBlocks;
         this.configuration = configuration.Value;
         data = (MongoData)storage;
      }

      public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         storageBatch.TotalSize += item.BlockInfo.Size;
         storageBatch.MapBlocks.Add(item.BlockInfo.Height, MongoStorageOperations.CreateMapBlock(item.BlockInfo));

         storageBatch.MapTransactionBlocks.AddRange(item.Transactions.Select(s => new MapTransactionBlock
         {
            BlockIndex = item.BlockInfo.Height,
            TransactionId = s.GetHash().ToString()
         }));

         if (configuration.StoreRawTransactions)
         {
            storageBatch.MapTransactions.AddRange(item.Transactions.Select(t => new MapTransaction
            {
               TransactionId = t.GetHash().ToString(),
               RawTransaction = t.ToBytes(syncConnection.Network.Consensus.ConsensusFactory)
            }));
         }

         IEnumerable<AddressForOutput> outputs = item.Transactions.SelectMany((trx, i) =>
            trx.Outputs.Select((output, index) =>
            {
               ScriptOutputTemplte res = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey);
               string addr = res != null ? (res?.Addresses != null && res.Addresses.Any()) ? res.Addresses.First() : res.TxOutType.ToString() : null;

               return new AddressForOutput
               {
                  Address = addr,
                  Outpoint = new Outpoint{TransactionId = trx.GetHash().ToString(), OutputIndex = index},
                  BlockIndex = item.BlockInfo.Height,
                  Value = output.Value,
                  ScriptHex = output.ScriptPubKey.ToHex(),
                  CoinBase = trx.IsCoinBase,
                  CoinStake = syncConnection.Network.Consensus.IsProofOfStake && trx.IsCoinStake,
               };
            }));


         storageBatch.AddressForOutputs.AddRange(outputs);

         var inputs = item.Transactions
            .Where(t => t.IsCoinBase == false)
            .SelectMany((trx, trxIndex) =>
               trx.Inputs.Select((input, inputIndex) =>
               {
                  return new AddressForInput()
                  {
                     Outpoint = new Outpoint
                     {
                        TransactionId = input.PrevOut.Hash.ToString(), OutputIndex = (int)input.PrevOut.N
                     },
                     TrxHash = trx.GetHash().ToString(),
                     BlockIndex = item.BlockInfo.Height,
                  };
               })).ToList();

         storageBatch.AddressForInputs.AddRange(inputs);
      }

      public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
      {
         if (syncingBlocks.IndexModeCompleted)
         {
            if (syncingBlocks.IbdMode() == false)
            {
               if (syncingBlocks.LocalMempoolView.Any())
               {
                  var toRemoveFromMempool = storageBatch.MapTransactionBlocks.Select(s => s.TransactionId).ToList();

                  FilterDefinitionBuilder<Mempool> builder = Builders<Mempool>.Filter;
                  FilterDefinition<Mempool> filter = builder.In(mempoolItem => mempoolItem.TransactionId, toRemoveFromMempool);

                  data.Mempool.DeleteMany(filter);

                  foreach (string mempooltrx in toRemoveFromMempool)
                     syncingBlocks.LocalMempoolView.Remove(mempooltrx, out _);
               }
            }
         }

         var t1 = Task.Run(() =>
         {
            data.MapBlock.InsertMany(storageBatch.MapBlocks.Values, new InsertManyOptions {IsOrdered = false });
         });

         var t2 = Task.Run(() =>
         {
            data.MapTransactionBlock.InsertMany(storageBatch.MapTransactionBlocks, new InsertManyOptions { IsOrdered = false });
         });

         var t3 = Task.Run(() =>
         {
            data.AddressForOutput.InsertMany(storageBatch.AddressForOutputs, new InsertManyOptions {IsOrdered = false});
         });

         var t4 = Task.Run(() =>
         {
            if (storageBatch.AddressForInputs.Any())
               data.AddressForInput.InsertMany(storageBatch.AddressForInputs, new InsertManyOptions {IsOrdered = false});
         });

         var t5 = Task.Run(() =>
         {
            try
            {
               if (storageBatch.MapTransactions.Any())
                  data.MapTransaction.InsertMany(storageBatch.MapTransactions, new InsertManyOptions {IsOrdered = false});
            }
            catch (MongoBulkWriteException mbwex)
            {
               // transactions are a special case they are not deleted from store in case of reorgs
               // because they will just be included in another blocks, so we ignore if key is already present
               if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))
               {
                  throw;
               }
            }
         });

         Task.WaitAll(t3, t4);

         var t6 = Task.Run(() =>
         {
            if (syncingBlocks.IndexModeCompleted)
            {
               PipelineDefinition<AddressForInput, AddressForInput> pipeline = BlockIndexer.BuildInputsAddressUpdatePiepline();
               data.AddressForInput.Aggregate(pipeline);
            }
         });

         Task.WaitAll(t1, t2, t3, t4, t5, t6);

         string lastBlockHash = null;
         long blockIndex = 0;
         var markBlocksAsComplete = new List<UpdateOneModel<MapBlock>>();
         foreach (MapBlock mapBlock in storageBatch.MapBlocks.Values.OrderBy(b => b.BlockIndex))
         {
            FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(block => block.BlockIndex, mapBlock.BlockIndex);
            UpdateDefinition<MapBlock> update = Builders<MapBlock>.Update.Set(blockInfo => blockInfo.SyncComplete, true);

            markBlocksAsComplete.Add(new UpdateOneModel<MapBlock>(filter, update));
            lastBlockHash = mapBlock.BlockHash;
            blockIndex = mapBlock.BlockIndex;
         }

         // mark each block is complete
         data.MapBlock.BulkWrite(markBlocksAsComplete, new BulkWriteOptions() { IsOrdered = true });

         SyncBlockInfo block = data.BlockByIndex(blockIndex);

         if (block.BlockHash != lastBlockHash)
         {
            throw new ArgumentException($"Expected hash {lastBlockHash} for block {blockIndex} but was {block.BlockHash}");
         }

         return block;
      }

      public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item)
      {
         var mempool = new List<Mempool>();
         var inputs = new Dictionary<string, (MempoolInput mempoolInput, Mempool mempool)>();

         foreach (Transaction itemTransaction in item.Transactions)
         {
            var mempoolEntry = new Mempool() {TransactionId = itemTransaction.GetHash().ToString()};
            mempool.Add(mempoolEntry);

            foreach (TxOut transactionOutput in itemTransaction.Outputs)
            {
               ScriptOutputTemplte res = ScriptToAddressParser.GetAddress(syncConnection.Network, transactionOutput.ScriptPubKey);
               string addr = res != null ? (res?.Addresses != null && res.Addresses.Any()) ? res.Addresses.First() : res.TxOutType.ToString() : null;

               if (addr != null)
               {
                  var output = new MempoolOutput {Value = transactionOutput.Value, ScriptHex = transactionOutput.ScriptPubKey.ToHex(), Address = addr};
                  mempoolEntry.Outputs.Add(output);
                  mempoolEntry.AddressOutputs.Add(addr);
               }
            }

            foreach (TxIn transactionInput in itemTransaction.Inputs)
            {
               var input = new MempoolInput {Outpoint = new Outpoint {OutputIndex = (int)transactionInput.PrevOut.N, TransactionId = transactionInput.PrevOut.Hash.ToString()}};
               mempoolEntry.Inputs.Add(input);
               inputs.Add($"{input.Outpoint.TransactionId}-{input.Outpoint.OutputIndex}", (input, mempoolEntry));
            }
         }

         List<AddressForOutput> outputsFromStore = FetchOutputs(inputs.Values.Select(s => s.mempoolInput.Outpoint).ToList());

         foreach (AddressForOutput outputFromStore in outputsFromStore)
         {
            if (inputs.TryGetValue($"{outputFromStore.Outpoint.TransactionId}-{outputFromStore.Outpoint.OutputIndex}", out (MempoolInput mempoolInput, Mempool mempool) input))
            {
               input.mempoolInput.Address = outputFromStore.Address;
               input.mempoolInput.Value = outputFromStore.Value;
               input.mempool.AddressInputs.Add(outputFromStore.Address);
            }
            else
            {
               // output not found
            }
         }

         data.Mempool.InsertMany(mempool, new InsertManyOptions { IsOrdered = false });

         foreach (Mempool mempooltrx in mempool)
            syncingBlocks.LocalMempoolView.TryAdd(mempooltrx.TransactionId, string.Empty);

         return new InsertStats {Items = mempool};
      }

      private List<AddressForOutput> FetchOutputs(List<Outpoint> outputs)
      {
         FilterDefinitionBuilder<AddressForOutput> builder = Builders<AddressForOutput>.Filter;
         FilterDefinition<AddressForOutput> filter = builder.In(output => output.Outpoint, outputs);

         var res = data.AddressForOutput.Find(filter).ToList();

         return res;
      }

      public static MapBlock CreateMapBlock(BlockInfo block)
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
   }
}
