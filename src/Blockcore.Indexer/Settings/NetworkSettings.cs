namespace Blockcore.Indexer.Settings
{
   public class NetworkSettings
   {
      public string NetworkConsensusFactoryType { get; set; }

      public byte NetworkPubkeyAddressPrefix { get; set; }

      public byte NetworkScriptAddressPrefix { get; set; }

      public string NetworkWitnessPrefix { get; set; }

      public int P2PPort { get; set; }

      public int RPCPort { get; set; }

      public int APIPort { get; set; }
   }
}
