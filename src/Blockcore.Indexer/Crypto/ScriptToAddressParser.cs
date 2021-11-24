using Blockcore.Consensus.ScriptInfo;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Indexer.Crypto
{
   public class ScriptOutputTemplte
   {
      public string[] Addresses { get; set; }

      public TxOutType TxOutType { get; set; }
   }

   public class ScriptToAddressParser
   {
      public static string GetSignerAddress(Network network, Script script)
      {
         BitcoinAddress address = script.GetSignerAddress(network);

         if (address == null)
         {
            return null;
         }

         return script.GetSignerAddress(network).ToString();
      }

      public static ScriptOutputTemplte GetAddress(Network network, Script script)
      {
         ScriptTemplate template = StandardScripts.GetTemplateFromScriptPubKey(script);

         if (template == null)
            return null;

         if (template.Type == TxOutType.TX_NONSTANDARD ||
             template.Type == TxOutType.TX_NULL_DATA ||
             template.Type == TxOutType.TX_MULTISIG)
         {
            return new ScriptOutputTemplte {TxOutType = template.Type};
         }

         if (template.Type == TxOutType.TX_PUBKEY)
         {
            PubKey[] pubkeys = script.GetDestinationPublicKeys(network);
            return new ScriptOutputTemplte
            {
               TxOutType = template.Type,
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
               return new ScriptOutputTemplte
               {
                  TxOutType = template.Type,
                  Addresses = new[] {bitcoinAddress.ToString()}
               };
            }
         }

         if (template.Type == TxOutType.TX_COLDSTAKE)
         {
            if (ColdStakingScriptTemplate.Instance.ExtractScriptPubKeyParameters(script, out KeyId hotPubKeyHash, out KeyId coldPubKeyHash))
            {
               // We want to index based on both the cold and hot key
               return new ScriptOutputTemplte
               {
                  TxOutType = template.Type,
                  Addresses = new[] { coldPubKeyHash.GetAddress(network).ToString(), hotPubKeyHash.GetAddress(network).ToString()}
               };
            }

            return new ScriptOutputTemplte { TxOutType = template.Type };
         }

         return null;
      }
   }
}
