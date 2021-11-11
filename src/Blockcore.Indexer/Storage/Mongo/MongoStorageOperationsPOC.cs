using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Crypto;
using Blockcore.Indexer.Operations;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;
using Blockcore.Utilities.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NBitcoin;
using NBitcoin.Crypto;

namespace Blockcore.Indexer.Storage.Mongo
{
   public class MongoStorageOperationsPOC : IStorageOperations
   {
      private readonly SyncConnection syncConnection;
      readonly IUtxoCache utxoCache;
      readonly IndexerSettings configuration;
      private readonly System.Diagnostics.Stopwatch watch;
      private readonly MongoData data;

      public MongoStorageOperationsPOC(SyncConnection syncConnection,IStorage storage, IUtxoCache utxoCache, IOptions<IndexerSettings> configuration)
      {
         this.syncConnection = syncConnection;
         this.utxoCache = utxoCache;
         this.configuration = configuration.Value;
         data = (MongoData)storage;
         watch = new System.Diagnostics.Stopwatch();
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
            trx.Outputs.Select((output,index) =>
               {
                  ScriptOutputTemplte res = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey);
                  string  addr = res != null ? res.Addresses.First() ?? res.TxOutType.ToString() : string.Empty;
                  
                  return new AddressForOutput
                  {
                     Address = addr,
                     Outpoint = $"{trx.GetHash()}-{index}",
                     BlockIndex = item.BlockInfo.Height,
                     Value = output.Value,
                     ScriptHex = output.ScriptPubKey.ToHex(),
                     CoinBase = trx.IsCoinBase,
                     CoinStake = syncConnection.Network.Consensus.IsProofOfStake && trx.IsCoinStake,
                  };
               })
               .Where(addr => addr.Address != null));
          

         storageBatch.AddressForOutputs.AddRange(outputs);
         utxoCache.AddToCache(storageBatch.AddressForOutputs);

         IEnumerable<AddressForInput> inputs = item.Transactions
            .Where(t => t.IsCoinBase == false)
            .SelectMany((trx, trxIndex) =>
               trx.Inputs.Select((input, inputIndex) =>
               {
                  string inputsOuput = $"{input.PrevOut.Hash}-{input.PrevOut.N}";

                  AddressForOutput utxo = utxoCache.GetOrFetch(inputsOuput);

                  return new AddressForInput()
                  {
                     Value = utxo.Value,
                     Address = utxo.Address,
                     Outpoint = inputsOuput,
                     TrxHash = trx.GetHash().ToString(),
                     BlockIndex = item.BlockInfo.Height,
                  };
               }))
            .Where(addr => addr.Address != null);

         var enumerated = inputs.ToList();
         storageBatch.AddressForInputs.AddRange(enumerated);
         utxoCache.RemoveFromCache(enumerated);

      }

      public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
      {
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
               // transactions are a special case where we ignore if key is already present
               if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey))
               {
                  throw;
               }
            }
         });

         string lastBlockHash = null;
         var markBlocksAsComplete = new List<UpdateOneModel<MapBlock>>();
         foreach (MapBlock mapBlock in storageBatch.MapBlocks.Values.OrderBy(b => b.BlockIndex))
         {
            FilterDefinition<MapBlock> filter = Builders<MapBlock>.Filter.Eq(block => block.BlockIndex, mapBlock.BlockIndex);
            UpdateDefinition<MapBlock> update = Builders<MapBlock>.Update.Set(blockInfo => blockInfo.SyncComplete, true);

            markBlocksAsComplete.Add(new UpdateOneModel<MapBlock>(filter, update));
            lastBlockHash = mapBlock.BlockHash;
         }

         Task.WaitAll(t1, t2, t3, t4, t5);

         // mark each block is complete
         data.MapBlock.BulkWrite(markBlocksAsComplete, new BulkWriteOptions() { IsOrdered = true });

         return data.BlockByHash(lastBlockHash);
      }

      public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item) => throw new System.NotImplementedException();
   }
}
