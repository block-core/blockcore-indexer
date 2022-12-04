using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

class StandardTokenSmartContractBuilder : ISmartContractBuilder<StandardTokenContractTable>
{
   readonly ICirrusMongoDb db;

   public StandardTokenSmartContractBuilder(ICirrusMongoDb db)
   {
      this.db = db;
   }

   public bool CanBuildSmartContract(string contractCodeType) => contractCodeType.Equals("StandardToken");

   public StandardTokenContractTable BuildSmartContract(CirrusContractTable createContractTransaction)
   {
      var logs = createContractTransaction.Logs.SingleOrDefault()?.Log;

      if (logs is null)
         throw new InvalidOperationException("Missing logs for create transaction of smart contract");

      return new StandardTokenContractTable
      {
         ContractAddress = createContractTransaction.NewContractAddress,
         ContractCreateTransactionId = createContractTransaction.TransactionId,
         LastProcessedBlockHeight = createContractTransaction.BlockIndex,
         Decimals = (logs.Data["tokenDecimals"].IsBsonNull ? 0 : logs.Data["tokenDecimals"].ToInt64()),
         Name = (string)logs.Data["tokenName"],
         Symbol = (string)logs.Data["tokenSymbole"],
         TotalSupply = logs.Data["tokenTotalSupply"].ToInt64(),
         CreatedOnBlock = createContractTransaction.BlockIndex,
         CreatorAddress = createContractTransaction.FromAddress,
      };
   }
}
