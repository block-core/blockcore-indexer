using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleToken;

public class NonFungibleTokenSmartContractBuilder : ISmartContractBuilder<NonFungibleTokenContractTable>
{
   public bool CanBuildSmartContract(string contractCodeType) => contractCodeType.Equals("NonFungibleToken");

   public NonFungibleTokenContractTable BuildSmartContract(CirrusContractTable createContractTransaction)
   {
      var logs = createContractTransaction.Logs.SingleOrDefault()?.Log;

      if (logs is null)
         throw new InvalidOperationException("Missing logs for create transaction of smart contract");

      return new()
      {
         ContractAddress = createContractTransaction.NewContractAddress,
         ContractCreateTransactionId = createContractTransaction.TransactionId,
         LastProcessedBlockHeight = createContractTransaction.BlockIndex,
         Name = (string)logs.Data["nftName"],
         Symbol =  (string)logs.Data["nftSymbol"],
         Owner = (string)logs.Data["nftOwner"],
         OwnerOnlyMinting = (bool)logs.Data["nftOwnerOnlyMinting"],
      };
   }
}
