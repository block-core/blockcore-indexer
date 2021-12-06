using Blockcore.Consensus.BlockInfo;
using Blockcore.Features.PoA;

namespace Cirrus
{
   public class SmartContractConsensusFactory : PoAConsensusFactory
   {
      /// <inheritdoc />
      public override BlockHeader CreateBlockHeader()
      {
         return new SmartContractPoABlockHeader();
      }
   }
}
