using Blockcore.Consensus.BlockInfo;

namespace Blockcore.Indexer.Cirrus
{
   public class SmartContractConsensusFactory : CollateralPoAConsensusFactory
   {
      /// <inheritdoc />
      public override BlockHeader CreateBlockHeader()
      {
         return new SmartContractPoABlockHeader();
      }
   }
}
