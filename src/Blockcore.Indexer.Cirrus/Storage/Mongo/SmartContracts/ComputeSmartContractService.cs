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

public class ComputeSmartContractService<T> : IComputeSmartContractService<T>
   where T : SmartContractComputedBase, new()
{
   readonly ILogger<ComputeSmartContractService<T>> logger;
   readonly ICirrusMongoDb mongoDb;
   readonly ISmartContractHandlersFactory<T> logReaderFactory;
   readonly CirrusClient cirrusClient;
   readonly IMongoDatabase mongoDatabase;
   readonly ISmartContractTransactionsLookup<T> transactionsLookup;

   readonly T emptyContract;

   public ComputeSmartContractService(ILogger<ComputeSmartContractService<T>> logger,
      ICirrusMongoDb db,
      ISmartContractHandlersFactory<T> logReaderFactory,
      ICryptoClientFactory clientFactory,
      SyncConnection connection,
      IMongoDatabase mongoDatabase,
      ISmartContractTransactionsLookup<T> transactionsLookup)
   {
      this.logger = logger;
      mongoDb = db;
      this.logReaderFactory = logReaderFactory;
      this.mongoDatabase = mongoDatabase;
      this.transactionsLookup = transactionsLookup;
      cirrusClient = (CirrusClient)clientFactory.Create(connection);
      emptyContract = new T();
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

   async Task<T> LookupSmartContractForAddressAsync(string address)
   {
      T contract = await GetSmartContractCollection()
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

      if (contract is not null)
         return contract;

      var contractCode = await mongoDb.CirrusContractCodeTable
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

      if (contractCode is null || contractCode.CodeType != emptyContract.ContractType)
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

   private async Task AddNewTransactionsDataToDocumentAsync(string address, List<CirrusContractTable> contractTransactions,
      T contract)
   {
      foreach (var contractTransaction in contractTransactions)
      {
         var reader = logReaderFactory.GetLogReader(contractTransaction.MethodName);

         if (reader is null)
         {
            logger.LogInformation($"No reader found for method {contractTransaction.MethodName} on transaction id - {contractTransaction.TransactionId}");
            throw new InvalidOperationException(
               $"Reader was not found for transaction - {contractTransaction.TransactionId}");
         }

         if (!reader.IsTransactionLogComplete(contractTransaction.Logs))
         {
            var result = await cirrusClient.GetContractInfoAsync(contractTransaction.TransactionId);

            contractTransaction.Logs = result.Logs;
         }

         reader.UpdateContractFromTransactionLog(contractTransaction, contract);

         contract.LastProcessedBlockHeight = contractTransaction.BlockIndex;
      }

      await SaveTheContractAsync(address, contract);
   }

   async Task SaveTheContractAsync(string address, T contract) =>
      await GetSmartContractCollection()
         .FindOneAndReplaceAsync<T>(_ => _.ContractAddress == address, contract,
         new FindOneAndReplaceOptions<T> { IsUpsert = true },
         CancellationToken.None);


   private IMongoCollection<T> GetSmartContractCollection()
   {
      return mongoDatabase.GetCollection<T>(typeof(T).Name.Replace("Table",string.Empty));
   }
}
