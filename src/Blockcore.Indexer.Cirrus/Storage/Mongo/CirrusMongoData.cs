using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Blockcore.Indexer.Core.Storage.Types;
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
         IScriptInterpeter scriptInterpeter,
         IMongoDatabase mongoDatabase)
         : base(
            dbLogger,
            connection,
            nakoConfiguration,
            chainConfiguration,
            globalState,
            mongoBlockToStorageBlock,
            clientFactory,
            scriptInterpeter,
            mongoDatabase)
      {
      }

      public IMongoCollection<CirrusContractTable> CirrusContractTable
      {
         get
         {
            return mongoDatabase.GetCollection<CirrusContractTable>("CirrusContract");
         }
      }

      public IMongoCollection<CirrusContractCodeTable> CirrusContractCodeTable
      {
         get
         {
            return mongoDatabase.GetCollection<CirrusContractCodeTable>("CirrusContractCode");
         }
      }


      protected override async Task OnDeleteBlockAsync(SyncBlockInfo block)
      {
         // delete the contracts
         FilterDefinition<CirrusContractTable> contractFilter = Builders<CirrusContractTable>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         Task<DeleteResult> contracts = CirrusContractTable.DeleteManyAsync(contractFilter);

         FilterDefinition<CirrusContractCodeTable> contractCodeFilter = Builders<CirrusContractCodeTable>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         Task<DeleteResult> contractsCode = CirrusContractCodeTable.DeleteManyAsync(contractCodeFilter);

         await Task.WhenAll(contracts, contractsCode);
      }

      public QueryContractCreate ContractCreate(string address)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = CirrusContractTable.AsQueryable()
            .Where(q => q.NewContractAddress == address);

         var res = cirrusContract.ToList();

         if (res.Count > 1)
            throw new ApplicationException("This is unexpected"); // todo: remove this temporary code

         return res.Select(item => new QueryContractCreate
         {
            Success = item.Success,
            ContractAddress = item.NewContractAddress,
            ContractCodeType = item.ContractCodeType,
            GasUsed = item.GasUsed,
            FromAddress = item.FromAddress,
            Error = item.Error,
            ContractOpcode = item.ContractOpcode,
            BlockIndex = item.BlockIndex,
            TransactionId = item.TransactionId
         }).FirstOrDefault();
      }

      public QueryResult<QueryContractCall> ContractCall(string address, int offset, int limit)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = CirrusContractTable.AsQueryable()
            .Where(q => q.ToAddress == address)
            .Skip(offset)
            .Take(limit);

         int total = CirrusContractTable.AsQueryable()
            .Where(q => q.ToAddress == address).Count();

         var res = cirrusContract.ToList();

         IEnumerable<QueryContractCall> transactions = res.Select(item => new QueryContractCall
         {
            Success = item.Success,
            MethodName = item.MethodName,
            ToAddress = item.NewContractAddress,
            GasUsed = item.GasUsed,
            FromAddress = item.FromAddress,
            Error = item.Error,
            BlockIndex = item.BlockIndex,
            TransactionId = item.TransactionId
         });

         return new QueryResult<QueryContractCall>
         {
            Items = transactions,
            Offset = offset,
            Limit = limit,
            Total = total
         };
      }

      public QueryContractTransaction ContractTransaction(string transacitonId)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = CirrusContractTable.AsQueryable()
            .Where(q => q.TransactionId == transacitonId);

         var res = cirrusContract.ToList();

         if (res.Count > 1)
            throw new ApplicationException("This is unexpected"); // todo: remove this temporary code

         return res.Select(item => new QueryContractTransaction
         {
            Success = item.Success,
            NewContractAddress = item.NewContractAddress,
            ContractCodeType = item.ContractCodeType,
            GasUsed = item.GasUsed,
            FromAddress = item.FromAddress,
            ToAddress = item.ToAddress,
            Logs= item.Logs,
            MethodName = item.MethodName,
            PostState = item.PostState,
            Error = item.Error,
            ContractOpcode = item.ContractOpcode,
            BlockIndex = item.BlockIndex,
            TransactionId = item.TransactionId
         }).FirstOrDefault();
      }

      public QueryContractCode ContractCode(string address)
      {
         IMongoQueryable<CirrusContractCodeTable> cirrusContractCode = CirrusContractCodeTable.AsQueryable()
            .Where(q => q.ContractAddress == address);

         var res = cirrusContractCode.ToList();

         return res.Select(item => new QueryContractCode
         {
            CodeType = item.CodeType,
            ByteCode = item.ByteCode,
            ContractHash = item.ContractHash,
            SourceCode = item.SourceCode
         }).FirstOrDefault();
      }
   }
}
