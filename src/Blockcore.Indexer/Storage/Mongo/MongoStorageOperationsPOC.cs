using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

         // var outputIndexList = item.Transactions.SelectMany((_,i) => _.Outputs.Select((o,j) =>
         //  new AddressForInput
         //  {
         //     UniquID = item.BlockInfo.Height * 100000000 + i * 1000 + j,
         //     OutputIndex = j,
         //     TransactionId = _.ToHex(),
         //     Address = o.ScriptPubKey.ToHex(),
         //  }));
         //
         //
         //
         // storageBatch.AddressForInputs.AddRange(outputIndexList);

         var addresses = item.Transactions.SelectMany((_, i) =>
               _.Outputs.Select((o, j) => new
               {
                  trx = _,
                  trxIndex = i,
                  addresses = ScriptToAddressParser.GetAddress(syncConnection.Network, o.ScriptPubKey),
                  outputIndex = j
               }))
            .Where(addr => addr.addresses != null)
            .SelectMany(addr =>
               addr.addresses.Select((a, addrIdx) => new AddressTransaction
               {
                  UniquId = item.BlockInfo.Height * 100000000 + addr.trxIndex * 100000 + addr.outputIndex * 100 +
                            addrIdx,
                  Address = a,
                  AddressHash = MemoryMarshal.AsRef<int>(Hashes.SHA256(Encoding.ASCII.GetBytes(a))),
                  TransactionId = addr.trx.GetHash().ToString(),
                  BlockIndex = item.BlockInfo.Height
               }));

         storageBatch.AddressTransactions.AddRange(addresses);
      }

      public SyncBlockInfo PushStorageBatch(StorageBatch storageBatch)
      {
         watch.Start();

         data.AddressTransaction.InsertMany(storageBatch.AddressTransactions, new InsertManyOptions { IsOrdered = false });

         watch.Stop();

         Console.WriteLine($"Inserts to Mongo {watch.Elapsed}");

         watch.Reset();

         return new SyncBlockInfo();
      }

      public InsertStats InsertMempoolTransactions(SyncBlockTransactionsOperation item) => throw new System.NotImplementedException();
   }
}
