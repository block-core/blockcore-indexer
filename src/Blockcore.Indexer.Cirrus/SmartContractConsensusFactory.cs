using Blockcore.Consensus.BlockInfo;

namespace Cirrus
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
