using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Client;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public class ComputeSmartContractServiceWithSplitDocuments<T,TDocument> : IComputeSmartContractService<T> where T : SmartContractTable, new() where TDocument : new()
{
   readonly ILogger<ComputeSmartContractServiceWithSplitDocuments<T,TDocument>> logger;
   readonly ICirrusMongoDb mongoDb;
   readonly ISmartContractHandlersFactory<T,TDocument> logReaderFactory;
   readonly CirrusClient cirrusClient;
   readonly IMongoDatabase mongoDatabase;
   readonly ISmartContractTransactionsLookup<T> transactionsLookup;

   public ComputeSmartContractServiceWithSplitDocuments(
      ILogger<ComputeSmartContractServiceWithSplitDocuments<T, TDocument>> logger,
      ICirrusMongoDb mongoDb, ISmartContractHandlersFactory<T, TDocument> logReaderFactory,
      ICryptoClientFactory clientFactory, SyncConnection connection, IMongoDatabase mongoDatabase,
      ISmartContractTransactionsLookup<T> transactionsLookup)
   {
      this.logger = logger;
      this.mongoDb = mongoDb;
      this.logReaderFactory = logReaderFactory;
      cirrusClient = (CirrusClient)clientFactory.Create(connection);
      ;
      this.mongoDatabase = mongoDatabase;
      this.transactionsLookup = transactionsLookup;
   }

   public async Task<T> ComputeSmartContractForAddressAsync(string address)
   {
      var contract = await LookupSmartContractForAddressAsync(address);

      if (contract is null)
         return null;

      var contractTransactions =
         await transactionsLookup.GetTransactionsForSmartContractAsync(address, contract.LastProcessedBlockHeight);

      if (contractTransactions.Any())
      {
         await AddNewTransactionsDataToDocumentAsync(address, contractTransactions, contract);
      }

      return contract;
   }

   async Task AddNewTransactionsDataToDocumentAsync(string address, List<CirrusContractTable> contractTransactions,
      T contract)
   {
      var writeModels = new List<WriteModel<TDocument>>();

      foreach (var contractTransaction in contractTransactions)
      {
         var reader = logReaderFactory.GetLogReader(contractTransaction.MethodName);

         if (reader is null)
         {
            logger.LogInformation(
               $"No reader found for method {contractTransaction.MethodName} on transaction id - {contractTransaction.TransactionId}");
            throw new InvalidOperationException(
               $"Reader was not found for transaction - {contractTransaction.TransactionId}");
         }

         if (!reader.IsTransactionLogComplete(contractTransaction.Logs))
         {
            var result = await cirrusClient.GetContractInfoAsync(contractTransaction.TransactionId);

            contractTransaction.Logs = result.Logs;
         }

         WriteModel<TDocument>[] instructions =
            reader.UpdateContractFromTransactionLog(contractTransaction, contract);

         if (instructions is not null)
            writeModels.AddRange(instructions);

         contract.LastProcessedBlockHeight = contractTransaction.BlockIndex;
      }

      await GetSmartContractCollection<T>()
         .FindOneAndReplaceAsync<T>(_ => _.ContractAddress == address, contract,
            new FindOneAndReplaceOptions<T> { IsUpsert = true },
            CancellationToken.None);

      var bulkWriteResult = await GetSmartContractTokenCollection<TDocument>()
         .BulkWriteAsync(writeModels, new BulkWriteOptions { IsOrdered = true });

      if (bulkWriteResult.ProcessedRequests.Count != writeModels.Count)
      {
         logger.LogError($"The bulk write operation on table {typeof(TDocument).Name}Table has failed");
         throw new Exception("The bulk write operation has failed");
      }
   }

   async Task<T> LookupSmartContractForAddressAsync(string address)
   {
      T contract = await GetSmartContractCollection<T>()
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

      if (contract is not null)
         return contract;

      var contractCode = await mongoDb.CirrusContractCodeTable
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

      if (contractCode is null )
      {
         logger.LogInformation(
            $"Request to compute smart contract for address {address} which was not found in the contract code table");
         return null;
      }

      contract = await CreateNewSmartContract(address);

      if (contract is null)
         throw new ArgumentNullException($"Contract not found in the contract table for address {address}");

      return contract;
   }

   private async Task<T> CreateNewSmartContract(string address)
   {
      var contractCreationTransaction = await mongoDb.CirrusContractTable
         .AsQueryable()
         .Where(_ => _.NewContractAddress == address)
         .SingleOrDefaultAsync();

      if (contractCreationTransaction is null)
         throw new ArgumentNullException(nameof(contractCreationTransaction));

      var builder = logReaderFactory.GetSmartContractBuilder(contractCreationTransaction.ContractCodeType);

      var contract = builder.BuildSmartContract(contractCreationTransaction);

      await SaveTheContractAsync(address, contract);

      return contract;
   }

   async Task SaveTheContractAsync(string address, T contract) =>
      await GetSmartContractCollection<T>()
         .FindOneAndReplaceAsync<T>(_ => _.ContractAddress == address, contract,
            new FindOneAndReplaceOptions<T> { IsUpsert = true },
            CancellationToken.None);

   private IMongoCollection<TType> GetSmartContractCollection<TType>()
   {
      return mongoDatabase.GetCollection<TType>(nameof(SmartContractTable));
   }

   private IMongoCollection<TType> GetSmartContractTokenCollection<TType>()
   {
      return mongoDatabase.GetCollection<TType>(typeof(TType).Name.Replace("Table",string.Empty));
   }
}
