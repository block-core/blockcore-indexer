using Blockcore.Indexer.Crypto;
using Blockcore.Indexer.Operations.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using Xunit;

namespace Blockcore.Indexer.Tests
{
   public class CityChainTests
   {
      [Fact]
      public void CreateTransactionFromHexAndVerifyInputAndOutputAddress()
      {
         IHost app = Program.CreateHostBuilder(new string[] { "--chain=CITY" }).Build();
         SyncConnection connection = app.Services.GetService<SyncConnection>();

         string transactionHex = "0100000017a7d45b01390aaba0b4ea98b86b0276795cb95419ec5a952039f7a38987e8b1b93c69d86c000000006a47304402201fd3c470ce90701e6c3f3211e5c92c49fb77926b9e522d9ec387c868255db0b90220698fe2c9b978b69af96f83db990e53bb5d90af342b9467c98ad50eefad9983a4012103d815bebdc04a6afe739764bc4afc12ddf6048b780c9e6dfd958084b1f65f2028ffffffff0244a1eb0b000000001976a9147a877414603e5566a5ce09a63188c97091f3ea1388ac0000b26c6200d0031976a9147fb4ccdfb80d6f9b55cf53cf94acddeeabc33a5088ac00000000";
         Transaction transaction = connection.Network.Consensus.ConsensusFactory.CreateTransaction(transactionHex);

         TxOut output1 = transaction.Outputs[0];
         TxIn input1 = transaction.Inputs[0];
         string[] address = ScriptToAddressParser.GetAddress(connection.Network, output1.ScriptPubKey);

         Assert.Equal("CTdmGuyx1DM2uWLSNR9LyHP7m6SE45n4z8", address[0]);
         Assert.Equal(199991620, output1.Value.Satoshi);

         string sender = ScriptToAddressParser.GetSignerAddress(connection.Network, input1.ScriptSig);
         Assert.Equal("CTdmGuyx1DM2uWLSNR9LyHP7m6SE45n4z8", sender);
      }
   }
}
