using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

class DaoSmartContractBuilder : ISmartContractBuilder<DaoContractTable>
{
   public bool CanBuildSmartContract(string contractCodeType) => contractCodeType.Equals("DAOContract");

   public DaoContractTable BuildSmartContract(CirrusContractTable createContractTransaction)
   {
      return new()
      {
         ContractAddress = createContractTransaction.NewContractAddress,
         ContractCreateTransactionId = createContractTransaction.TransactionId,
         LastProcessedBlockHeight = createContractTransaction.BlockIndex
      };
   }
}
