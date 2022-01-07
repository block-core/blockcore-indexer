using System;
using System.Linq;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoData : MongoData // todo: make an ICirrusStorage interface
   {
      public CirrusMongoData(
         ILogger<MongoDb> dbLogger,
         SyncConnection connection,
         IOptions<IndexerSettings> nakoConfiguration,
         IOptions<ChainSettings> chainConfiguration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         ICryptoClientFactory clientFactory,
         IScriptInterpeter scriptInterpeter)
         : base(
            dbLogger,
            connection,
            nakoConfiguration,
            chainConfiguration,
            globalState,
            mongoBlockToStorageBlock,
            clientFactory,
            scriptInterpeter)
      {
      }

      public IMongoCollection<CirrusContractTable> CirrusContractTable
      {
         get
         {
            return mongoDatabase.GetCollection<CirrusContractTable>("CirrusContract");
         }
      }

      public QueryAddressContract AddressContract(string address)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = CirrusContractTable.AsQueryable()
            .Where(q => q.NewContractAddress == address);

         var res = cirrusContract.ToList();

         if (res.Count > 1)
            throw new ApplicationException("This is unexpected"); // todo: remove this temporary code

         return res.Select(item => new QueryAddressContract
         {
            Success = item.Success,
            NewContractAddress = item.NewContractAddress,
            GasUsed = item.GasUsed,
            FromAddress = item.FromAddress,
            Error = item.Error,
            ContractType = item.ContractType,
            BlockIndex = item.BlockIndex,
            ToAddress = item.ToAddress,
            TransactionId = item.TransactionId
         }).FirstOrDefault();
      }

      public QueryAddressContract TransactionContract(string transacitonId)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = CirrusContractTable.AsQueryable()
            .Where(q => q.TransactionId == transacitonId);

         var res = cirrusContract.ToList();

         if (res.Count > 1)
            throw new ApplicationException("This is unexpected"); // todo: remove this temporary code

         return res.Select(item => new QueryAddressContract
         {
            Success = item.Success,
            NewContractAddress = item.NewContractAddress,
            GasUsed = item.GasUsed,
            FromAddress = item.FromAddress,
            Error = item.Error,
            ContractType = item.ContractType,
            BlockIndex = item.BlockIndex,
            ToAddress = item.ToAddress,
            TransactionId = item.TransactionId
         }).FirstOrDefault();
      }

   }
}
