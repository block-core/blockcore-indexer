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

public class NftComputationService : IComputeSmartContractService<NonFungibleTokenComputedTable>
{
   readonly ILogger<NftComputationService> logger;
   readonly ICirrusMongoDb mongoDb;
   readonly ISmartContractHandlersFactory<NonFungibleTokenComputedTable> logReaderFactory;
   readonly CirrusClient cirrusClient;

   public NftComputationService(ILogger<NftComputationService> logger,
      ICirrusMongoDb mongoDb,
      ISmartContractHandlersFactory<NonFungibleTokenComputedTable> logReaderFactory,
      ICryptoClientFactory clientFactory,
      SyncConnection connection)
   {
      this.logger = logger;
      this.mongoDb = mongoDb;
      this.logReaderFactory = logReaderFactory;
      cirrusClient = (CirrusClient)clientFactory.Create(connection);
   }

   public async Task<NonFungibleTokenComputedTable> ComputeSmartContractForAddressAsync(string address)
   {
      var contract = await LookupSmartContractForAddressAsync(address);

      if (contract is null)
         return null;

      var contractTransactions = await mongoDb.CirrusContractTable
         .AsQueryable()
         .Where(_ => (_.ToAddress == address ||
                      _.Logs.Any(_ => _.Log.Data.ContainsKey("contract") && _.Log.Data["contract"] == address) ) &&
                     (_.Success && _.BlockIndex > contract.LastProcessedBlockHeight ))
         .ToListAsync();

      if (contractTransactions.Any())
      {
         await AddNewTransactionsDataToDocumentAsync(address, contractTransactions, contract);
      }

      return contract;
   }


   async Task<NonFungibleTokenComputedTable> LookupSmartContractForAddressAsync(string address)
   {
      NonFungibleTokenComputedTable contract = await mongoDb.NonFungibleTokenComputedTable
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

      if (contract is not null)
         return contract;

      var contractCode = await mongoDb.CirrusContractCodeTable
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

      if (contractCode is null || contractCode.CodeType != "NonFungibleToken")
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

   private async Task<NonFungibleTokenComputedTable> CreateNewSmartContract(string address)
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
      NonFungibleTokenComputedTable contract)
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

   async Task SaveTheContractAsync(string address, NonFungibleTokenComputedTable contract) =>
      await mongoDb.NonFungibleTokenComputedTable
         .FindOneAndReplaceAsync<NonFungibleTokenComputedTable>(_ => _.ContractAddress == address, contract,
         new FindOneAndReplaceOptions<NonFungibleTokenComputedTable> { IsUpsert = true },
         CancellationToken.None);

}
