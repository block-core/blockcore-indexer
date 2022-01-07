using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Cirrus.Client;
using Blockcore.Indexer.Cirrus.Client.Types;
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
      readonly CirrusMongoData cirrusMongoData;

      public CirrusMongoStorageOperations(
         SyncConnection syncConnection,
         IStorage storage,
         IUtxoCache utxoCache,
         IOptions<IndexerSettings> configuration,
         GlobalState globalState,
         IMapMongoBlockToStorageBlock mongoBlockToStorageBlock,
         IScriptInterpeter scriptInterpeter,
         ICryptoClientFactory clientFactory) :
         base(
             syncConnection,
             storage,
             utxoCache,
             configuration,
             globalState,
             mongoBlockToStorageBlock,
             scriptInterpeter)
      {
         this.clientFactory = clientFactory;
         cirrusClient = this.clientFactory.Create(syncConnection) as CirrusClient;
         cirrusMongoData = storage as CirrusMongoData;
      }

      protected override void OnAddToStorageBatch(StorageBatch storageBatch, SyncBlockTransactionsOperation item)
      {
         storageBatch.ExtraData ??= new CirrusStorageBatch();

         if (!(storageBatch.ExtraData is CirrusStorageBatch cirrusStorageBatch))
            throw new ArgumentNullException(nameof(cirrusStorageBatch));

         foreach (Transaction transaction in item.Transactions)
         {
            TxOut smartContractTxOut = transaction.Outputs.FirstOrDefault(txOut => txOut.ScriptPubKey.IsSmartContractExec());

            if (smartContractTxOut != null)
            {
               // this is a smart contract transaction

               if (smartContractTxOut.ScriptPubKey.IsSmartContractCreate())
               {
                  // fetch the create contract receipt
                  ReceiptResponse receipt = cirrusClient.GetReceiptAsync(transaction.GetHash().ToString()).Result;
                  
                  // todo: later combine this two endpoint to a single endpoint
                  string contractType = null;
                  if (receipt.Success)
                  {
                     contractType = cirrusClient.GetContractCodeAsync(receipt.NewContractAddress).Result?.Type;
                  }

                  cirrusStorageBatch.CirrusContractTable.Add(new CirrusContractTable
                  {
                     ContractType = contractType,
                     NewContractAddress = receipt.NewContractAddress,
                     FromAddress = receipt.From,
                     ToAddress = receipt.To,
                     BlockIndex = item.BlockInfo.Height,
                     TransactionId = receipt.TransactionHash,
                     Success = receipt.Success,
                     Error = receipt.Error,
                     GasUsed = receipt.GasUsed,
                  });

               }
            }
         }
      }

      protected override void OnPushStorageBatch(StorageBatch storageBatch)
      {
         if (!(storageBatch.ExtraData is CirrusStorageBatch cirrusStorageBatch))
            throw new ArgumentNullException(nameof(cirrusStorageBatch));

         var t1 = Task.Run(() =>
         {
            if (cirrusStorageBatch.CirrusContractTable.Any())
               cirrusMongoData.CirrusContractTable.InsertMany(cirrusStorageBatch.CirrusContractTable, new InsertManyOptions { IsOrdered = false });
         });

         Task.WaitAll(t1);

      }
   }
}
