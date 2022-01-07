using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Core.Crypto;
using Blockcore.Networks;

namespace Blockcore.Indexer.Cirrus.Crypto
{
   public class CirrusScriptToAddressParser : ScriptToAddressParser
   {
      public override ScriptOutputInfo InterpretScript(Network network, Script script)
      {
         ScriptOutputInfo ret = base.InterpretScript(network, script);

         if (ret != null)
            return ret;

         if (SmartContractScript.IsSmartContractCreate(new Script(script.ToBytes())))
         {
            return new ScriptOutputInfo { ScriptType = ScOpcodeType.OP_CREATECONTRACT.ToString() };
         }

         if (SmartContractScript.IsSmartContractCall(new Script(script.ToBytes())))
         {
            return new ScriptOutputInfo { ScriptType = ScOpcodeType.OP_CALLCONTRACT.ToString() };
         }

         if (SmartContractScript.IsSmartContractSpend(new Script(script.ToBytes())))
         {
            return new ScriptOutputInfo { ScriptType = ScOpcodeType.OP_SPEND.ToString() };
         }

         if (SmartContractScript.IsSmartContractInternalCall(new Script(script.ToBytes())))
         {
            return new ScriptOutputInfo { ScriptType = ScOpcodeType.OP_INTERNALCONTRACTTRANSFER.ToString() };

         }

         return null;
      }
   }
}
