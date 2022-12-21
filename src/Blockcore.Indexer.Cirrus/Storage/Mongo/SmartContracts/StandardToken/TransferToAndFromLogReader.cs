using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

public class TransferToAndFromLogReader : LogReaderBase,ILogReader<StandardTokenContractTable, StandardTokenHolderTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferTo") || methodType.Equals("TransferFrom");

   public override List<LogType> RequiredLogs { get; set; } = new (){ LogType.TransferLog };

   public WriteModel<StandardTokenHolderTable>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenContractTable computedTable)
   {
      if (contractTransaction.Logs == null || !contractTransaction.Logs.Any())
      {
         return Array.Empty<WriteModel<StandardTokenHolderTable>>(); //Need to understand why this happens (example transaction id - a03a7c0122668c9c6d5a7bb47494f903a89519d8fbec2df135eb6687047c58d5)
      }

      var transferLog = GetLogByType(LogType.TransferLog, contractTransaction.Logs);

      string fromAddress = (string)transferLog.Log.Data["from"];
      string toAddress = (string)transferLog.Log.Data["to"];
      object objectAmount = transferLog.Log.Data["amount"];

      long? amount = objectAmount switch
      {
         string => Convert.ToInt64(objectAmount),
         long l => l,
         _ => throw new InvalidCastException(objectAmount.ToString())
      };

      var addCreatorTokenIfNotExists = new UpdateOneModel<StandardTokenHolderTable>(Builders<StandardTokenHolderTable>.Filter
            .Where(_ => _.Id.TokenId == computedTable.CreatorAddress &&
                        _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<StandardTokenHolderTable>.Update.SetOnInsert(_ => _.Id.TokenId, computedTable.CreatorAddress)
            .SetOnInsert(_ => _.Id.ContractAddress, computedTable.ContractAddress)
            .SetOnInsert(_ => _.AmountChangesHistory,
               new List<StandardTokenAmountChange>
               {
                  new ()
                  {
                     Amount = computedTable.TotalSupply,
                     BlockIndex = computedTable.CreatedOnBlock,
                     TransactionId = computedTable.ContractCreateTransactionId
                  }
               }));

      addCreatorTokenIfNotExists.IsUpsert = true;

      var addToAddressIfNotExists = new UpdateOneModel<StandardTokenHolderTable>(Builders<StandardTokenHolderTable>.Filter
            .Where(_ => _.Id.TokenId == toAddress && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<StandardTokenHolderTable>.Update.SetOnInsert(_ => _.Id.TokenId, toAddress)
            .SetOnInsert(_ => _.Id.ContractAddress, computedTable.ContractAddress));

      addToAddressIfNotExists.IsUpsert = true;

      return new WriteModel<StandardTokenHolderTable>[]
      {
         addCreatorTokenIfNotExists,
         addToAddressIfNotExists,
         new UpdateOneModel<StandardTokenHolderTable>(Builders<StandardTokenHolderTable>.Filter
               .Where(_ => _.Id.TokenId == fromAddress && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<StandardTokenHolderTable>.Update.AddToSet(_ => _.AmountChangesHistory, new StandardTokenAmountChange{Amount = -amount ?? 0,BlockIndex = contractTransaction.BlockIndex,TransactionId = contractTransaction.TransactionId})),
         new UpdateOneModel<StandardTokenHolderTable>(Builders<StandardTokenHolderTable>.Filter
               .Where(_ => _.Id.TokenId == toAddress && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<StandardTokenHolderTable>.Update.AddToSet(_ => _.AmountChangesHistory, new StandardTokenAmountChange{Amount = amount ?? 0,BlockIndex = contractTransaction.BlockIndex,TransactionId = contractTransaction.TransactionId}))
      };
   }
}
