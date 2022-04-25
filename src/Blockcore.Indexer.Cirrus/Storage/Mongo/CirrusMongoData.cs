using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoData : MongoData, ICirrusStorage
   {
      readonly ICirrusMongoDb mongoDb;
      readonly IComputeSmartContractService<NonFungibleTokenComputedTable> smartContractService;

      public CirrusMongoData(
         ILogger<MongoDb> dbLogger,
         SyncConnection connection,
         IOptions<ChainSettings> chainConfiguration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         ICryptoClientFactory clientFactory,
         IScriptInterpeter scriptInterpeter,
         IMongoDatabase mongoDatabase,
         ICirrusMongoDb db,
         IComputeSmartContractService<NonFungibleTokenComputedTable> smartContractService)
         : base(
            dbLogger,
            connection,
            chainConfiguration,
            globalState,
            mongoBlockToStorageBlock,
            clientFactory,
            scriptInterpeter,
            mongoDatabase,
            db)
      {
         mongoDb = db;
         this.smartContractService = smartContractService;
      }

      protected override async Task OnDeleteBlockAsync(SyncBlockInfo block)
      {
         // delete the contracts
         FilterDefinition<CirrusContractTable> contractFilter = Builders<CirrusContractTable>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         Task<DeleteResult> contracts = mongoDb.CirrusContractTable.DeleteManyAsync(contractFilter);

         FilterDefinition<CirrusContractCodeTable> contractCodeFilter = Builders<CirrusContractCodeTable>.Filter.Eq(info => info.BlockIndex, block.BlockIndex);
         Task<DeleteResult> contractsCode = mongoDb.CirrusContractCodeTable.DeleteManyAsync(contractCodeFilter);

         await Task.WhenAll(contracts, contractsCode);
      }

      public QueryContractCreate ContractCreate(string address)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = mongoDb.CirrusContractTable.AsQueryable()
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
            GasPrice = item.GasPrice,
            Amount = item.Amount,
            FromAddress = item.FromAddress,
            Error = item.Error,
            ContractOpcode = item.ContractOpcode,
            BlockIndex = item.BlockIndex,
            TransactionId = item.TransactionId
         }).FirstOrDefault();
      }

      public QueryResult<QueryContractCall> ContractCall(string address, string filterAddress, int? offset, int limit)
      {
         IMongoQueryable<CirrusContractTable> totalQuary = mongoDb.CirrusContractTable.AsQueryable()
             .Where(q => q.ToAddress == address);

         if (filterAddress != null)
         {
            totalQuary = totalQuary.Where(q => q.FromAddress == filterAddress);
         }

         int total = totalQuary.Count();

         IMongoQueryable<CirrusContractTable> cirrusContract = mongoDb.CirrusContractTable.AsQueryable()
            .Where(q => q.ToAddress == address);

         if (filterAddress != null)
         {
            cirrusContract = cirrusContract.Where(q => q.FromAddress == filterAddress);
         }

         int itemsToSkip = offset ?? (total < limit ? 0 : total - limit);

         cirrusContract = cirrusContract
            .OrderBy(b => b.BlockIndex)
            .Skip(itemsToSkip)
            .Take(limit);

         var res = cirrusContract.ToList();

         IEnumerable<QueryContractCall> transactions = res.Select(item => new QueryContractCall
         {
            Success = item.Success,
            MethodName = item.MethodName,
            ToAddress = item.NewContractAddress,
            GasUsed = item.GasUsed,
            GasPrice = item.GasPrice,
            Amount = item.Amount,
            FromAddress = item.FromAddress,
            Error = item.Error,
            BlockIndex = item.BlockIndex,
            TransactionId = item.TransactionId
         });

         return new QueryResult<QueryContractCall>
         {
            Items = transactions,
            Offset = itemsToSkip,
            Limit = limit,
            Total = total
         };
      }

      public QueryContractTransaction ContractTransaction(string transacitonId)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = mongoDb.CirrusContractTable.AsQueryable()
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
            GasPrice = item.GasPrice,
            Amount = item.Amount,
            FromAddress = item.FromAddress,
            ToAddress = item.ToAddress,
            Logs = item.Logs,
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
         IMongoQueryable<CirrusContractCodeTable> cirrusContractCode = mongoDb.CirrusContractCodeTable.AsQueryable()
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

      public async Task<QueryResult<QueryAddressAsset>> GetAssetsForAddressAsync(string address, int? offset, int limit)
      {
         int total = await mongoDb.NonFungibleTokenComputedTable
            .AsQueryable()
            .SumAsync(_ => _.Tokens.Count(t => t.Owner == address));

         int startPosition = offset ?? total - limit;
         int endPosition = startPosition + limit;

         var contracts = await mongoDb.NonFungibleTokenComputedTable.Aggregate()
            .Match(_ => _.Tokens.Any(t => t.Owner == address))
            .Unwind(_ => _.Tokens)
            .Match(_ => _["Tokens"]["Owner"] == address)
            .Skip(startPosition)
            .Limit(endPosition)
            .ToListAsync();

         var tokens =
            contracts.Select(contract =>
               new QueryAddressAsset
                  {
                     Creator = contract["Tokens"]["Creator"].AsString,
                     ContractId = contract["_id"].AsString,
                     Id = contract["Tokens"]["_id"].AsString,
                     Uri = contract["Tokens"]["Uri"].AsString,
                     IsBurned = contract["Tokens"]["IsBurned"].AsBoolean,
                     TransactionId =
                        contract["Tokens"]["SalesHistory"].AsBsonArray.Any()
                           ? contract["Tokens"]["SalesHistory"].AsBsonArray.Last()["TransactionId"].AsString
                           : null,
                     PricePaid = GetPricePaidFromHistory(contract["Tokens"]["SalesHistory"].AsBsonArray)
                  });

         return new QueryResult<QueryAddressAsset>
         {
            Items = tokens, Limit = limit, Offset = offset ?? 0, Total = total
         };
      }

      private static long? GetPricePaidFromHistory(BsonArray saleEvents)
      {
         if (!saleEvents.Any())
            return null;

         var last = saleEvents.Last();
         return last["_t"].AsBsonArray.Last().AsString switch
         {
            nameof(Auction) => last["HighestBid"].AsInt64,
            nameof(OnSale) => last["Price"].AsInt64,
            _ => 0
         };
      }
   }
}
