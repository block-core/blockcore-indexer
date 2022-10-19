using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.Internal;
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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoData : MongoData, ICirrusStorage
   {
      readonly ICirrusMongoDb mongoDb;

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
         IBlockRewindOperation rewindOperation)
         : base(
            dbLogger,
            connection,
            chainConfiguration,
            globalState,
            mongoBlockToStorageBlock,
            clientFactory,
            scriptInterpeter,
            mongoDatabase,
            db,
            rewindOperation)
      {
         mongoDb = db;
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

      public QueryResult<QueryContractGroup> GroupedContracts()
      {
         var groupedContracts = mongoDb.CirrusContractCodeTable.Aggregate()
            .Group(_ => _.CodeType, ac => new QueryContractGroup
            {
               ContractCodeType = ac.Key,
               Count = ac.Count(),
               ContractHash = ac.First().ContractHash
            })
            .ToList();

         return new QueryResult<QueryContractGroup>
         {
            Items = groupedContracts,
            Offset = 0,
            Limit = groupedContracts.Count,
            Total = groupedContracts.Count
         };
      }

      public QueryResult<QueryContractList> ListContracts(string contractType, int? offset, int limit)
      {
         IMongoQueryable<CirrusContractTable> totalQuary = mongoDb.CirrusContractTable.AsQueryable()
            .Where(q => q.ContractOpcode == "create" && q.ContractCodeType == contractType && q.Success == true);

         int total = totalQuary.Count();

         int itemsToSkip = offset ?? (total < limit ? 0 : total - limit);

         IMongoQueryable<CirrusContractTable> cirrusContract = mongoDb.CirrusContractTable.AsQueryable()
            .Where(q => q.ContractOpcode == "create" &&  q.ContractCodeType == contractType && q.Success == true)
            .OrderBy(b => b.BlockIndex)
            .Skip(itemsToSkip)
            .Take(limit);

         var res = cirrusContract.ToList();

         IEnumerable<QueryContractList> transactions = res.Select(item => new QueryContractList
         {
            ContractAddress = item.NewContractAddress,
            ContractCodeType = item.ContractCodeType,
            Error = item.Error,
            BlockIndex = item.BlockIndex,
            TransactionId = item.TransactionId
         });

         return new QueryResult<QueryContractList>
         {
            Items = transactions,
            Offset = itemsToSkip,
            Limit = limit,
            Total = total
         };
      }

      public async Task<QueryDAOContract> GetDaoContractByAddressAsync(string contractAddress)
      {
         var contract = await mongoDb.DaoContractTable.Find(_ => _.ContractAddress == contractAddress)
            .SingleOrDefaultAsync();

         if (contract is null)
            return null;

         var tokens = await mongoDb.DaoContractProposalTable.Find(_ => _.Id.ContractAddress == contractAddress)
            .ToListAsync();

         return new QueryDAOContract
         {
            Deposits = contract.Deposits,
            Proposals = tokens,
            ApprovedAddresses = contract.ApprovedAddresses,
            CurrentAmount = contract.CurrentAmount,
            WhitelistedCount = contract.WhitelistedCount,
            MaxVotingDuration = contract.MaxVotingDuration,
            MinVotingDuration = contract.MaxVotingDuration
         };
      }

      public async Task<QueryStandardTokenContract> GetStandardTokenContractByAddressAsync(string contractAddress)
      {
         var contract = await mongoDb.SmartContractTable.Find(_ => _.ContractAddress == contractAddress)
            .SingleOrDefaultAsync() as StandardTokenContractTable;

         if (contract is null)
            return null;

         var tokens = await mongoDb.StandardTokenHolderTable.Find(_ => _.Id.ContractAddress == contractAddress)
            .ToListAsync();

         return new QueryStandardTokenContract
         {
            tokens = tokens,
            Decimals = contract.Decimals,
            Name = contract.Name,
            Symbol = contract.Symbol,
            CreatorAddress = contract.CreatorAddress,
            TotalSupply = contract.TotalSupply,
            CreatedOnBlock = contract.CreatedOnBlock
         };
      }

      public async Task<QueryNonFungibleTokenContract> GetNonFungibleTokenContractByAddressAsync(string contractAddress)
      {
         var contract = await mongoDb.NonFungibleTokenContractTable.Find(_ => _.ContractAddress == contractAddress)
            .SingleOrDefaultAsync();

         if (contract is null)
            return null;

         var tokens = await mongoDb.NonFungibleTokenTable.Find(_ => _.Id.ContractAddress == contractAddress)
            .ToListAsync();

         return new QueryNonFungibleTokenContract
         {
            Name = contract.Name,
            Owner = contract.Owner,
            Symbol = contract.Symbol,
            Tokens = tokens,
            PendingOwner = contract.PendingOwner,
            PreviousOwners = contract.PreviousOwners,
            OwnerOnlyMinting = contract.OwnerOnlyMinting
         };
      }

      public Task<NonFungibleTokenTable> GetNonFungibleTokenByIdAsync(string contractAddress, string tokenId)
      {
         return mongoDb.NonFungibleTokenTable.Find(_ =>
               _.Id.ContractAddress == contractAddress && _.Id.TokenId == tokenId)
            .FirstOrDefaultAsync();
      }

      public async Task<QueryStandardToken> GetStandardTokenByIdAsync(string contractAddress, string tokenId)
      {
         var token = await mongoDb.StandardTokenContractTable.Find(_ => _.ContractAddress == contractAddress)
            .FirstOrDefaultAsync();
         var tokenAmounts = await mongoDb.StandardTokenHolderTable.Find(_ => _.Id.ContractAddress == contractAddress &&
                                                           _.Id.TokenId == tokenId)
            .FirstOrDefaultAsync();

         return new QueryStandardToken
         {
            Name = token.Name,
            Symbol = token.Symbol,
            TotalSupply = token.TotalSupply,
            Address = tokenAmounts.Id.TokenId,
            Amount = tokenAmounts.AmountChangesHistory.Sum(_ => _.Amount)
         };
      }

      public QueryContractCreate ContractCreate(string address)
      {
         IMongoQueryable<CirrusContractTable> cirrusContract = mongoDb.CirrusContractTable.AsQueryable()
            .Where(q => q.NewContractAddress == address);

         var res = cirrusContract.ToList();

         if (res.Count > 1)
            throw new ApplicationException("This is unexpected"); // todo: remove this temporary code

         CirrusContractTable lastEntry = mongoDb.CirrusContractTable
            .AsQueryable()
            .OrderByDescending(b => b.BlockIndex)
            .FirstOrDefault(q => q.ToAddress == address);


         return res.Select(item => new QueryContractCreate
         {
            Success = item.Success,
            ContractAddress = item.NewContractAddress,
            ContractCodeType = item.ContractCodeType,
            GasUsed = item.GasUsed,
            GasPrice = item.GasPrice,
            Amount = item.Amount,
            ContractBalance = lastEntry?.ContractBalance ?? 0,
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
            ContractBalance = item.ContractBalance,
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
            ContractBalance = item.ContractBalance,
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

      public async Task<QueryResult<QueryAddressAsset>> GetNonFungibleTokensForAddressAsync(string address, int? offset, int limit)
      {
         int total = await mongoDb.NonFungibleTokenTable
            .AsQueryable()
            .CountAsync(_ => _.Owner == address);

         int startPosition = offset ?? total - limit;

         var dbTokens = await mongoDb.NonFungibleTokenTable.Aggregate()
            .Match(_ => _.Owner == address)
            //.SortBy(_ => _.) TODO David check if we need sorting for the FE
            .Skip(startPosition)
            .Limit(limit)
            .ToListAsync();

         var tokens = dbTokens.Select(_ => new QueryAddressAsset
         {
            Creator = _.Creator,
            ContractId = _.Id.ContractAddress,
            Id = _.Id.TokenId,
            Uri = _.Uri,
            IsBurned = _.IsBurned,
            TransactionId = _.SalesHistory.LastOrDefault()?.TransactionId,
            PricePaid = _.SalesHistory.LastOrDefault() switch
            {
               Auction auction => auction.HighestBid,
               OnSale sale => sale.Price,
               _ => 0
            }
         });

         return new QueryResult<QueryAddressAsset>
         {
            Items = tokens, Limit = limit, Offset = offset ?? 0, Total = total
         };
      }

      public async Task<QueryResult<QueryStandardToken>> GetStandardTokensForAddressAsync(string address, int? offset, int limit)
      {
         int total = await mongoDb.StandardTokenHolderTable
            .AsQueryable()
            .CountAsync(_ => _.Id.TokenId == address);

         if (total == 0)
            return new QueryResult<QueryStandardToken> { Limit = limit, Offset = offset ?? 0, Total = total };

         int startPosition = offset ?? total - limit;

         var dbTokens = await mongoDb.StandardTokenHolderTable.Aggregate()
            .Match(_ => _.Id.TokenId == address)
            .SortBy(_ => _.Id.ContractAddress)
            .Skip(startPosition)
            .Limit(limit)
            .ToListAsync();

         var addresses = dbTokens.Select(_ => _.Id.ContractAddress);

         var smartContractDetails = await mongoDb.StandardTokenContractTable.AsQueryable()
            .Where(_ => addresses.Contains(_.ContractAddress))
            .ToListAsync();

         var tokens = dbTokens.Select(_ =>
         {
            var smartContract = smartContractDetails.First(s => s.ContractAddress == _.Id.ContractAddress);

            return new QueryStandardToken
            {
               Address = _.Id.TokenId,
               Amount = _.AmountChangesHistory.Sum(a => a.Amount),
               Name = smartContract.Name,
               Symbol = smartContract.Symbol,
               TotalSupply = smartContract.TotalSupply,
               Decimals = smartContract.Decimals
            };
         });

         return new QueryResult<QueryStandardToken>
         {
            Items = tokens, Limit = limit, Offset = offset ?? 0, Total = total
         };
      }

      public async Task<List<SmartContractTable>> GetSmartContractsThatNeedsUpdatingAsync(long blockIndex)
      {
         var smartContractsNotComputed = await mongoDb.CirrusContractCodeTable.Aggregate()
            .Lookup(mongoDb.SmartContractTable.CollectionNamespace.CollectionName,
               new StringFieldDefinition<CirrusContractCodeTable>(nameof(CirrusContractCodeTable.ContractAddress)),
               new StringFieldDefinition<SmartContractTable>(nameof(SmartContractTable.ContractAddress)),
               new StringFieldDefinition<SmartContractTable[]>("output"))
            .Unwind("output")
            .Match(_ => _["output"].IsBsonNull ||
                        _["output.BlockIndex"] < blockIndex)
            .ReplaceRoot(_ => _["output"])
            .As<SmartContractTable>()
            .ToListAsync();

         var smartContractsNotUpdated = await mongoDb.CirrusContractTable.Aggregate(PipelineDefinition<CirrusContractTable,SmartContractTable>.Create(
            new []
            {
               new BsonDocument("$match",
                  new BsonDocument { { "Success", true }, { "ToAddress", new BsonDocument("$ne", BsonNull.Value) } }),
               new BsonDocument("$group",
                  new BsonDocument
                  {
                     { "_id", "$ToAddress" },
                     { "ContractCodeType", new BsonDocument("$first", "$ContractCodeType") },
                     { "BlockIndex", new BsonDocument("$max", "$BlockIndex") }
                  }),
               new BsonDocument("$lookup",
                  new BsonDocument
                  {
                     { "from", "SmartContractTable" },
                     { "localField", "_id" },
                     { "foreignField", "_id" },
                     { "as", "output" }
                  }),
               new BsonDocument("$match",
                  new BsonDocument("output",
                     new BsonDocument("$ne",
                        new BsonArray()))),
               new BsonDocument("$unwind",
                  new BsonDocument { { "path", "$output" }, { "preserveNullAndEmptyArrays", false } }),
               new BsonDocument("$match",
                  new BsonDocument("$expr",
                     new BsonDocument("$gt",
                        new BsonArray { "$BlockIndex", "$output.LastProcessedBlockHeight" }))),
               new BsonDocument("$replaceRoot",
                  new BsonDocument("newRoot", "$output"))
            }))
            .ToListAsync();

         return smartContractsNotComputed.Concat(smartContractsNotUpdated).ToList();
      }

      public async Task<List<string>> GetSmartContractsThatNeedsUpdatingAsync(params string[] supportedTypes)
      {
         var smartContractsNotComputed = await mongoDb.CirrusContractCodeTable.Aggregate()
            .Match(Builders<CirrusContractCodeTable>.Filter.In(_ => _.CodeType, supportedTypes))
            .Lookup("SmartContractTable",
               new StringFieldDefinition<CirrusContractCodeTable>("ContractAddress"),
               new StringFieldDefinition<BsonDocument>("_id"),
               new StringFieldDefinition<BsonDocument>("output"))
            .Match(_ => _["output"] == new BsonArray())
            .Project(_ => new { address = _["ContractAddress"] })
            .ToListAsync();

         var smartContractsNotUpdated = await mongoDb.CirrusContractTable.Aggregate(PipelineDefinition<CirrusContractTable,BsonDocument>.Create(
               new []
               {
                  new BsonDocument("$match",
                     new BsonDocument { { "Success", true }, { "ToAddress", new BsonDocument("$ne", BsonNull.Value) } }),
                  new BsonDocument("$group",
                     new BsonDocument
                     {
                        { "_id", "$ToAddress" },
                        { "ContractCodeType", new BsonDocument("$first", "$ContractCodeType") },
                        { "BlockIndex", new BsonDocument("$max", "$BlockIndex") }
                     }),
                  new BsonDocument("$lookup",
                     new BsonDocument
                     {
                        { "from", "SmartContractTable" },
                        { "localField", "_id" },
                        { "foreignField", "_id" },
                        { "as", "output" }
                     }),
                  new BsonDocument("$match",
                     new BsonDocument("CodeType",
                        new BsonDocument("$nin",
                           new BsonArray(supportedTypes)))),
                  new BsonDocument("$match",
                     new BsonDocument("output",
                        new BsonDocument("$ne",
                           new BsonArray()))),
                  new BsonDocument("$unwind",
                     new BsonDocument { { "path", "$output" }, { "preserveNullAndEmptyArrays", false } }),
                  new BsonDocument("$match",
                     new BsonDocument("$expr",
                        new BsonDocument("$gt",
                           new BsonArray { "$BlockIndex", "$output.LastProcessedBlockHeight" }))),
                  new BsonDocument("$project",
                     new BsonDocument("address", "$ContractAddress"))
               }))
            .ToListAsync();

         return smartContractsNotUpdated.Select(_ => _["_id"].AsString).ToList().Concat(smartContractsNotComputed.Select(s => s.address.AsString)).ToList();
      }
   }
}
