using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Crypto;
using Blockcore.Indexer.Operations;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Storage.Mongo.Types;
using Blockcore.Indexer.Storage.Types;
using Blockcore.Utilities.Extensions;
using MongoDB.Driver;
using NBitcoin.Crypto;

namespace Blockcore.Indexer.Storage.Mongo
{
   public class MongoStorageOperationsPOC : IStorageOperations
   {
      private readonly SyncConnection syncConnection;
      private readonly System.Diagnostics.Stopwatch watch;
      private readonly MongoData data;

      public MongoStorageOperationsPOC(SyncConnection syncConnection,IStorage storage)
      {
         this.syncConnection = syncConnection;
         data = (MongoData)storage;
         watch = new System.Diagnostics.Stopwatch();
      }

      public void AddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         storageBatch.TotalSize += item.BlockInfo.Size;
         storageBatch.MapBlocks.Add(item.BlockInfo.Height, new MapBlock {BlockIndex = item.BlockInfo.Height, BlockHash = item.BlockInfo.Hash, PreviousBlockHash = item.BlockInfo.PreviousBlockHash});// CreateMapBlock(item.BlockInfo));

         var outputs = item.Transactions.SelectMany((trx, i) =>
            trx.Outputs.Select((output, j) =>
               {
                  return new AddressForOutput
                  {
                     Address = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey)?.FirstOrDefault(),
                     Outpoint = string.Format("{0}-{1}", trx.GetHash(), j),
                     BlockIndex = item.BlockInfo.Height,
                     Value = output.Value,
                     ScriptHex = output.ScriptPubKey.ToHex(),
                     CoinBase = trx.IsCoinBase,
                     CoinStake = syncConnection.Network.Consensus.IsProofOfStake && trx.IsCoinStake,
                  };
               })
               .Where(addr => addr.Address != null));
          

         storageBatch.AddressForOutputs.AddRange(outputs);

         var inputs = item.Transactions.SelectMany((trx, i) =>
            trx.Inputs.Select((output, j) =>
            {
               return new AddressForInput()
               {
                  //Address = ScriptToAddressParser.GetAddress(syncConnection.Network, output.ScriptPubKey).FirstOrDefault(),
                  Outpoint = string.Format("{0}-{1}", trx.GetHash(), j), BlockIndex = item.BlockInfo.Height,
                  //Value = output.Value,
               };
            }));


         storageBatch.AddressForInputs.AddRange(inputs);
      }

      public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
      {
         watch.Start();

         var t1 = Task.Run(() =>
         {
            try
            {
               data.AddressForOutput.InsertMany(storageBatch.AddressForOutputs, new InsertManyOptions {IsOrdered = false});
            }
            catch (MongoBulkWriteException mbwex)
            {
               if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey)) //.Message.Contains("E11000 duplicate key error collection"))
               {
                  throw;
               }
            }
         });

         var t2 = Task.Run(() =>
         {
            try
            {
               if (storageBatch.AddressForInputs.Any())
                  data.AddressForInput.InsertMany(storageBatch.AddressForInputs, new InsertManyOptions {IsOrdered = false});
            }
            catch (MongoBulkWriteException mbwex)
            {
               if (mbwex.WriteErrors.Any(e => e.Category != ServerErrorCategory.DuplicateKey)) //.Message.Contains("E11000 duplicate key error collection"))
               {
                  throw;
               }
            }
         });

         Task.WaitAll(t1, t2);

         watch.Stop();

         Console.WriteLine($"Inserts to Mongo {watch.Elapsed}");

         watch.Reset();

         return new SyncBlockInfo();
      }

      public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item) => throw new System.NotImplementedException();
   }
}
