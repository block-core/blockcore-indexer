using System;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Client;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public class DaoContractAggregator : IDAOContractAggregator
{
   const string DaoContract = "DAOContract";

   readonly ILogger<DaoContractAggregator> logger;
   readonly CirrusMongoData mongoData;
   readonly ILogReaderFactory logReaderFactory;
   readonly CirrusClient cirrusClient;

   public DaoContractAggregator(ILogger<DaoContractAggregator> logger, ICirrusStorage mongoData, ILogReaderFactory logReaderFactory, ICryptoClientFactory clientFactory, SyncConnection connection)
   {
      this.mongoData = (CirrusMongoData)mongoData;
      this.logReaderFactory = logReaderFactory;
      this.logger = logger;

      cirrusClient = clientFactory.Create(connection) as CirrusClient;
   }

   public async Task<DaoContractComputedTable> ComputeDaoContractForAddressAsync(string address)
   {
      var contract = await mongoData.DaoContractComputedTable
         .AsQueryable()
         .SingleOrDefaultAsync(_ => _.ContractAddress == address);

       if (contract is null)
      {
         var contractCode = await mongoData.CirrusContractCodeTable
            .AsQueryable()
            .SingleOrDefaultAsync(_ => _.ContractAddress == address);

         if (contractCode is null || contractCode.CodeType != DaoContract)
         {
            logger.LogInformation("TODO");
            return null;
         }

         var contractCreationTransaction = await mongoData.CirrusContractTable
            .AsQueryable()
            .Where(_ => _.NewContractAddress == address)
            .SingleOrDefaultAsync();

         if (contractCreationTransaction is null)
            throw new ArgumentNullException(nameof(contractCreationTransaction));

         contract = new DaoContractComputedTable
         {
            ContractAddress = contractCreationTransaction.NewContractAddress,
            LasProcessedBlockHeight = -1
            //TODO
         };
      }

      var contractTransactions = await mongoData.CirrusContractTable
         .AsQueryable()
         .Where(_ => _.ToAddress == address && _.Success && _.BlockIndex > contract.LasProcessedBlockHeight)
         .ToListAsync();

      foreach (var contractTransaction in contractTransactions)
      {
         var reader = logReaderFactory.GetLogReader(contractTransaction.MethodName);

         if (reader is null)
         {
            Console.WriteLine(contractTransaction.MethodName);
            continue;
         }

         if (!reader.IsTheTransactionLogComplete(contractTransaction.Logs))
         {
            var result = await cirrusClient.GetContractInfoAsync(contractTransaction.TransactionId);

            contractTransaction.Logs = result.Logs;
         }

         reader.UpdateContractFromTransactionLog(contractTransaction, contract);

         contract.LasProcessedBlockHeight = contractTransaction.BlockIndex;
      }

      await mongoData.DaoContractComputedTable.FindOneAndReplaceAsync<DaoContractComputedTable>(
         _ => _.ContractAddress == address, contract,
         new FindOneAndReplaceOptions<DaoContractComputedTable> { IsUpsert = true },
         CancellationToken.None);

      return contract;
   }
}
