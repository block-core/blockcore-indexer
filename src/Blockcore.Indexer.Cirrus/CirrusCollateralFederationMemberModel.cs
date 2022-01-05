namespace Blockcore.Indexer.Cirrus
{
   public class CirrusCollateralFederationMemberModel : Blockcore.Features.PoA.CollateralFederationMemberModel
   {
      public string PubKeyHex { get; set; }

      public long CollateralAmountSatoshis { get; set; }

      public string CollateralMainchainAddress { get; set; }
   }
}
