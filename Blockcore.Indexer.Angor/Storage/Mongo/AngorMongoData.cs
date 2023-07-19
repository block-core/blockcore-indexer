using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Sync;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Angor.Storage.Mongo;

public class AngorMongoData : MongoData, IAngorStorage
{
   readonly IAngorMongoDb mongoDb;

   public AngorMongoData(ILogger<AngorMongoDb> dbLogger, SyncConnection connection, IOptions<ChainSettings> chainConfiguration,
      GlobalState globalState, IMapMongoBlockToStorageBlock mongoBlockToStorageBlock, ICryptoClientFactory clientFactory,
      IScriptInterpeter scriptInterpeter, IMongoDatabase mongoDatabase, IMongoDb db, IBlockRewindOperation rewindOperation,
      IComputeHistoryQueue computeHistoryQueue, IAngorMongoDb mongoDb)
      : base(dbLogger, connection, chainConfiguration, globalState, mongoBlockToStorageBlock, clientFactory,
         scriptInterpeter, mongoDatabase, db, rewindOperation, computeHistoryQueue)
   {
      this.mongoDb = mongoDb;
   }


}
