using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.OPDEX;

public class OpdexMindTokenSmartContractBuilder : ISmartContractBuilder<OpdexMinedTokenContractTable>
{
   public bool CanBuildSmartContract(string contractCodeType) =>
      contractCodeType.Equals("OpdexMinedToken");

   public OpdexMinedTokenContractTable BuildSmartContract(CirrusContractTable createContractTransaction)
   {
      return new OpdexMinedTokenContractTable
      {
         ContractAddress = createContractTransaction.NewContractAddress,
         CreatorAddress = createContractTransaction.FromAddress,
         CreatedOnBlock = createContractTransaction.BlockIndex,
         ContractCreateTransactionId = createContractTransaction.TransactionId,
         LastProcessedBlockHeight = createContractTransaction.BlockIndex
      };
   }
}
