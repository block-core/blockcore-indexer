using System;
using System.Text;
using Blockcore.Features.PoA;
using Blockcore.Utilities.JsonConverters;
using NBitcoin;

namespace Cirrus
{
   public class CollateralPoAConsensusFactory : PoAConsensusFactory
   {
      public override IFederationMember DeserializeFederationMember(byte[] serializedBytes)
      {
         string json = Encoding.ASCII.GetString(serializedBytes);

         CirrusCollateralFederationMemberModel model = Serializer.ToObject<CirrusCollateralFederationMemberModel>(json);

         var member = new CollateralFederationMember(new PubKey(model.PubKeyHex),
            new Money(model.CollateralAmountSatoshis), model.CollateralMainchainAddress);

         return member;
      }

      public override byte[] SerializeFederationMember(IFederationMember federationMember)
      {
         var member = federationMember as CollateralFederationMember;

         if (member == null)
            throw new ArgumentException($"Member of type: '{nameof(CollateralFederationMember)}' should be provided.");

         // Guard.Assert(!member.IsMultisigMember);

         var model = new Blockcore.Features.PoA.CollateralFederationMemberModel()
         {
            CollateralMainchainAddress = member.CollateralMainchainAddress,
            CollateralAmountSatoshis = member.CollateralAmount,
            PubKeyHex = member.PubKey.ToHex()
         };

         string json = Serializer.ToString(model);

         byte[] jsonBytes = Encoding.ASCII.GetBytes(json);

         return jsonBytes;
      }
   }
}
