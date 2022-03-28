using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class DaoSmartContractBuilder : ISmartContractBuilder<DaoContractComputedTable>
{
   public bool CanBuildSmartContract(string contractCodeType) => contractCodeType.Equals("DAOContract");

   public DaoContractComputedTable BuildSmartContract(CirrusContractTable createContractTransaction)
   {
      return new()
      {
         ContractAddress = createContractTransaction.NewContractAddress,
         ContractCreateTransactionId = createContractTransaction.TransactionId,
         LastProcessedBlockHeight = createContractTransaction.BlockIndex
      };
   }
}
