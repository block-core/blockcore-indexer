namespace Blockcore.Indexer.Cirrus
{
   public class CirrusCollateralFederationMemberModel : Blockcore.Features.PoA.CollateralFederationMemberModel
   {
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
      public string PubKeyHex { get; set; }
      public long CollateralAmountSatoshis { get; set; }
      public string CollateralMainchainAddress { get; set; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
   }
}
