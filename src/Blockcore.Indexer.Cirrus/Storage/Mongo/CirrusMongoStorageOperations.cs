using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Cirrus.Client;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Crypto;
using Blockcore.Indexer.Cirrus.Operations.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo
{
   public class CirrusMongoStorageOperations : MongoStorageOperations
   {
      private readonly ICryptoClientFactory clientFactory;
      private readonly CirrusClient cirrusClient;
      readonly ICirrusMongoDb cirrusdDb;

      public CirrusMongoStorageOperations(
         SyncConnection syncConnection,
         IStorage storage,
         IOptions<IndexerSettings> configuration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         IScriptInterpreter scriptInterpeter,
         ICryptoClientFactory clientFactory,
         ICirrusMongoDb db) :
         base(
             syncConnection,
             db,
             configuration,
             globalState,
             mongoBlockToStorageBlock,
             scriptInterpeter,
             storage)
      {
         this.clientFactory = clientFactory;
         cirrusClient = this.clientFactory.Create(syncConnection) as CirrusClient;
         cirrusdDb = db;
      }

      protected override void OnAddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         CirrusStorageBatch cirrusStorageBatch = storageBatch as CirrusStorageBatch;

         foreach (Transaction transaction in item.Transactions)
         {
            TxOut smartContractInternalCallTxOut = transaction.Outputs.FirstOrDefault(txOut => txOut.ScriptPubKey.IsSmartContractInternalCall());
            if (smartContractInternalCallTxOut != null)
            {
               // handle sc internal transfer
            }

            TxOut smartContractTxOut = transaction.Outputs.FirstOrDefault(txOut => txOut.ScriptPubKey.IsSmartContractExec());

            if (smartContractTxOut != null)
            {
               // is this a smart contract transaction
               if (smartContractTxOut.ScriptPubKey.IsSmartContractExec())
               {
                  string contractOpcode = smartContractTxOut.ScriptPubKey.IsSmartContractCreate() ? "create" :
                     smartContractTxOut.ScriptPubKey.IsSmartContractCall() ? "call" : null;

                  // fetch the contract receipt
                  ContractReceiptResponse receipt = cirrusClient.GetContractInfoAsync(transaction.GetHash().ToString()).Result;

                  if (receipt == null)
                  {
                     throw new ApplicationException($"Smart Contract receipt not found for trx {transaction.GetHash()}");
                  }

                  cirrusStorageBatch.CirrusContractTable.Add(new CirrusContractTable
                  {
                     ContractOpcode = contractOpcode,
                     ContractCodeType = receipt.ContractCodeType,
                     MethodName = receipt.MethodName,
                     NewContractAddress = receipt.NewContractAddress,
                     FromAddress = receipt.From,
                     ToAddress = receipt.To,
                     BlockIndex = item.BlockInfo.Height,
                     BlockHash = item.BlockInfo.Hash,
                     TransactionId = receipt.TransactionHash,
                     Success = receipt.Success,
                     Error = receipt.Error,
                     PostState = receipt.PostState,
                     GasUsed = receipt.GasUsed,
                     GasPrice = receipt.GasPrice,
                     Amount = receipt.Amount,
                     ContractBalance = receipt.ContractBalance,
                     Logs = receipt.Logs
                  });

                  if (receipt.ContractCodeHash != null)
                  {
                     cirrusStorageBatch.CirrusContractCodeTable.Add(new CirrusContractCodeTable
                     {
                        ContractAddress = receipt.NewContractAddress,
                        BlockIndex = item.BlockInfo.Height,
                        CodeType = receipt.ContractCodeType,
                        ContractHash = receipt.ContractCodeHash,
                        ByteCode = receipt.ContractBytecode,
                        SourceCode = receipt.ContractCSharp
                     });
                  }
               }
            }
         }
      }

      protected override void OnPushStorageBatch(StorageBatch storageBatch)
      {
         CirrusStorageBatch cirrusStorageBatch = storageBatch as CirrusStorageBatch;

         var t1 = Task.Run(() =>
         {
            if (cirrusStorageBatch.CirrusContractTable.Any())
               cirrusdDb.CirrusContractTable.InsertMany(cirrusStorageBatch.CirrusContractTable, new InsertManyOptions { IsOrdered = false });
         });

         var t2 = Task.Run(() =>
         {
            if (cirrusStorageBatch.CirrusContractCodeTable.Any())
               cirrusdDb.CirrusContractCodeTable.InsertMany(cirrusStorageBatch.CirrusContractCodeTable, new InsertManyOptions { IsOrdered = false });
         });


         Task.WaitAll(t1, t2);
      }
   }
}
