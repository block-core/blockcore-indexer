using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.StandardToken;

public class TransferToAndFromLogReader : ILogReader<StandardTokenComputedTable, StandardTokenHolder>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("TransferTo") || methodType.Equals("TransferFrom");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs?.Any(_ => _.Log.Data.ContainsKey("from") &&
                                                                             _.Log.Data.ContainsKey("to") &&
                                                                             _.Log.Data.ContainsKey("amount")) ?? false;

   public WriteModel<StandardTokenHolder>[] UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      StandardTokenComputedTable computedTable)
   {
      if (contractTransaction.Logs == null || !contractTransaction.Logs.Any())
      {
         return Array.Empty<WriteModel<StandardTokenHolder>>(); //Need to understand why this happens (example transaction id - a03a7c0122668c9c6d5a7bb47494f903a89519d8fbec2df135eb6687047c58d5)
      }

      string fromAddress = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["from"];
      string toAddress = (string)contractTransaction.Logs.SingleOrDefault().Log.Data["to"];
      long? amount = (long?)contractTransaction.Logs.SingleOrDefault().Log.Data["amount"];

      var addCreatorTokenIfNotExists = new UpdateOneModel<StandardTokenHolder>(Builders<StandardTokenHolder>.Filter
            .Where(_ => _.Id.TokenId == computedTable.CreatorAddress &&
                        _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<StandardTokenHolder>.Update.SetOnInsert(_ => _.Id.TokenId, computedTable.CreatorAddress)
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

      var addToAddressIfNotExists = new UpdateOneModel<StandardTokenHolder>(Builders<StandardTokenHolder>.Filter
            .Where(_ => _.Id.TokenId == toAddress && _.Id.ContractAddress == computedTable.ContractAddress),
         Builders<StandardTokenHolder>.Update.SetOnInsert(_ => _.Id.TokenId, toAddress)
            .SetOnInsert(_ => _.Id.ContractAddress, computedTable.ContractAddress));

      addToAddressIfNotExists.IsUpsert = true;

      return new WriteModel<StandardTokenHolder>[]
      {
         addCreatorTokenIfNotExists,
         addToAddressIfNotExists,
         new UpdateOneModel<StandardTokenHolder>(Builders<StandardTokenHolder>.Filter
               .Where(_ => _.Id.TokenId == fromAddress && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<StandardTokenHolder>.Update.AddToSet(_ => _.AmountChangesHistory, new StandardTokenAmountChange{Amount = -amount ?? 0,BlockIndex = contractTransaction.BlockIndex,TransactionId = contractTransaction.TransactionId})),
         new UpdateOneModel<StandardTokenHolder>(Builders<StandardTokenHolder>.Filter
               .Where(_ => _.Id.TokenId == toAddress && _.Id.ContractAddress == computedTable.ContractAddress),
            Builders<StandardTokenHolder>.Update.AddToSet(_ => _.AmountChangesHistory, new StandardTokenAmountChange{Amount = amount ?? 0,BlockIndex = contractTransaction.BlockIndex,TransactionId = contractTransaction.TransactionId}))
      };
   }
}
