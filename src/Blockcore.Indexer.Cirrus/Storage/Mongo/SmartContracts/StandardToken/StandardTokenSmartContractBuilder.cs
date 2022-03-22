using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

class StandardTokenSmartContractBuilder : ISmartContractBuilder<StandardTokenComputedTable>
{
   public bool CanBuildSmartContract(string contractCodeType) => contractCodeType.Equals("StandardToken");

   public StandardTokenComputedTable BuildSmartContract(CirrusContractTable createContractTransaction)
   {
      var logs = createContractTransaction.Logs.SingleOrDefault()?.Log;

      if (logs is null)
         throw new InvalidOperationException("Missing logs for create transaction of smart contract");

      return new()
      {
         ContractAddress = createContractTransaction.NewContractAddress,
         ContractCreateTransactionId = createContractTransaction.TransactionId,
         LastProcessedBlockHeight = createContractTransaction.BlockIndex,
         Decimals = (logs.Data["tokenDecimals"] == null ? 0 : (long)logs.Data["tokenDecimals"]),
         Name = (string)logs.Data["tokenName"],
         Symbol = (string)logs.Data["tokenSymbole"],
         TotalSupply = (long)logs.Data["tokenTotalSupply"],
         TokenHolders = new List<StandardTokenHolder>
         {
            new()
            {
               Address = createContractTransaction.FromAddress,
               Amount = (long)logs.Data["tokenTotalSupply"]
            }
         }
      };
   }
}
