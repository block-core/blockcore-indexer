using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Core.Storage.Mongo
{
   public class MongoDb : IMongoDb
   {
      private readonly ILogger<MongoDb> log;

      protected readonly IMongoDatabase mongoDatabase;

      public MongoDb(ILogger<MongoDb> logger, IMongoDatabase mongoDatabase)
      {
         log = logger;
         this.mongoDatabase = mongoDatabase;
      }


      public IMongoCollection<OutputTable> OutputTable
      {
         get
         {
            return mongoDatabase.GetCollection<OutputTable>("Output");
         }
      }

      public IMongoCollection<InputTable> InputTable
      {
         get
         {
            return mongoDatabase.GetCollection<InputTable>("Input");
         }
      }

      public IMongoCollection<UnspentOutputTable> UnspentOutputTable
      {
         get
         {
            return mongoDatabase.GetCollection<UnspentOutputTable>("UnspentOutput");
         }
      }

      public IMongoCollection<AddressComputedTable> AddressComputedTable
      {
         get
         {
            return mongoDatabase.GetCollection<AddressComputedTable>("AddressComputed");
         }
      }

      public IMongoCollection<AddressHistoryComputedTable> AddressHistoryComputedTable
      {
         get
         {
            return mongoDatabase.GetCollection<AddressHistoryComputedTable>("AddressHistoryComputed");
         }
      }

      public IMongoCollection<TransactionBlockTable> TransactionBlockTable
      {
         get
         {
            return mongoDatabase.GetCollection<TransactionBlockTable>("TransactionBlock");
         }
      }

      public IMongoCollection<TransactionTable> TransactionTable
      {
         get
         {
            return mongoDatabase.GetCollection<TransactionTable>("Transaction");
         }
      }

      public IMongoCollection<BlockTable> BlockTable
      {
         get
         {
            return mongoDatabase.GetCollection<BlockTable>("Block");
         }
      }

      public IMongoCollection<RichlistTable> RichlistTable
      {
         get
         {
            return mongoDatabase.GetCollection<RichlistTable>("RichList");
         }
      }

      public IMongoCollection<PeerInfo> Peer
      {
         get
         {
            return mongoDatabase.GetCollection<PeerInfo>("Peer");
         }
      }

      public IMongoCollection<MempoolTable> Mempool
      {
         get
         {
            return mongoDatabase.GetCollection<MempoolTable>("Mempool");
         }
      }

      public IMongoCollection<ReorgBlockTable> ReorgBlock
      {
         get
         {
            return mongoDatabase.GetCollection<ReorgBlockTable>("ReorgBlock");
         }
      }
   }
}
