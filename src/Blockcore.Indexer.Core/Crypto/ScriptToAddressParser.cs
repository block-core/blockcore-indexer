using Blockcore.Consensus.ScriptInfo;
using Blockcore.NBitcoin;
using Blockcore.Networks;

namespace Blockcore.Indexer.Core.Crypto
{
   public class ScriptOutputInfo
   {
      public string[] Addresses { get; set; }

      public string ScriptType { get; set; }
   }

   public interface IScriptInterpeter
   {
      string GetSignerAddress(Network network, Script script);
      ScriptOutputInfo InterpretScript(Network network, Script script);
   }

   public class ScriptToAddressParser : IScriptInterpeter
   {
      public virtual ScriptOutputInfo InterpretScript(Network network, Script script) => GetAddressInternal(network, script);

      public virtual string GetSignerAddress(Network network, Script script) => GetSignerAddressInternal(network, script);

      public static string GetSignerAddressInternal(Network network, Script script)
      {
         BitcoinAddress address = script.GetSignerAddress(network);

         if (address == null)
         {
            return null;
         }

         return script.GetSignerAddress(network).ToString();
      }

      public static ScriptOutputInfo GetAddressInternal(Network network, Script script)
      {
         ScriptTemplate template = StandardScripts.GetTemplateFromScriptPubKey(script);

         if (template == null)
            return null;

         if (template.Type == TxOutType.TX_NONSTANDARD ||
             template.Type == TxOutType.TX_NULL_DATA ||
             template.Type == TxOutType.TX_MULTISIG)
         {
            return new ScriptOutputInfo {ScriptType = template.Type.ToString()};
         }

         if (template.Type == TxOutType.TX_PUBKEY)
         {
            PubKey[] pubkeys = script.GetDestinationPublicKeys(network);
            return new ScriptOutputInfo
            {
               ScriptType = template.Type.ToString(),
               Addresses = new[] {pubkeys[0].GetAddress(network).ToString()}
            };
         }

         if (template.Type == TxOutType.TX_PUBKEYHASH ||
             template.Type == TxOutType.TX_SCRIPTHASH ||
             template.Type == TxOutType.TX_SEGWIT)
         {
            BitcoinAddress bitcoinAddress = script.GetDestinationAddress(network);
            if (bitcoinAddress != null)
            {
               return new ScriptOutputInfo
               {
                  ScriptType = template.Type.ToString(),
                  Addresses = new[] {bitcoinAddress.ToString()}
               };
            }
         }

         if (template.Type == TxOutType.TX_COLDSTAKE)
         {
            if (ColdStakingScriptTemplate.Instance.ExtractScriptPubKeyParameters(script, out KeyId hotPubKeyHash, out KeyId coldPubKeyHash))
            {
               // We want to index based on both the cold and hot key
               return new ScriptOutputInfo
               {
                  ScriptType = template.Type.ToString(),
                  Addresses = new[] { coldPubKeyHash.GetAddress(network).ToString(), hotPubKeyHash.GetAddress(network).ToString()}
               };
            }

            return new ScriptOutputInfo { ScriptType = template.Type.ToString() };
         }

         return null;
      }
   }
}
