using Blockcore.Indexer.Client.Types;
using Blockcore.Indexer.Operations.Types;
using Blockcore.Indexer.Settings;
using Blockcore.Indexer.Storage.Mongo.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Storage.Mongo
{
   public class MongoDb
   {
      private readonly ILogger<MongoDb> log;

      protected readonly MongoClient mongoClient;

      protected readonly IMongoDatabase mongoDatabase;

      protected readonly SyncConnection syncConnection;
      protected readonly GlobalState globalState;

      private readonly IndexerSettings configuration;

      protected readonly ChainSettings chainConfiguration;

      public MongoDb(ILogger<MongoDb> logger, SyncConnection connection, IOptions<IndexerSettings> nakoConfiguration, IOptions<ChainSettings> chainConfiguration, GlobalState globalState)
      {
         configuration = nakoConfiguration.Value;
         this.chainConfiguration = chainConfiguration.Value;

         syncConnection = connection;
         this.globalState = globalState;
         log = logger;
         mongoClient = new MongoClient(configuration.ConnectionString.Replace("{Symbol}", this.chainConfiguration.Symbol.ToLower()));

         string dbName = configuration.DatabaseNameSubfix ? "Blockchain" + this.chainConfiguration.Symbol : "Blockchain";

         mongoDatabase = mongoClient.GetDatabase(dbName);
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
            return mongoDatabase.GetCollection<MapRichlist>("RichList");
         }
      }

      public IMongoCollection<PeerInfo> Peer
      {
         get
         {
            return mongoDatabase.GetCollection<PeerInfo>("Peer");
         }
      }

      public IMongoCollection<Mempool> Mempool
      {
         get
         {
            return mongoDatabase.GetCollection<Mempool>("Mempool");
         }
      }
   }
}
