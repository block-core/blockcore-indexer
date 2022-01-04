namespace Blockcore.Indexer.Storage.Mongo.Types
{
   using System.Collections.Generic;

   public class MempoolOutput
   {
      public string ScriptHex { get; set; }

      public long Value { get; set; }

      public string Address { get; set; }

   }

   public class MempoolInput
   {
      public long Value { get; set; }

      public string Address { get; set; }

      public Outpoint Outpoint { get; set; }
   }

   public class MempoolTable
   {
      public string Id { get; set; }

      public List<string> AddressOutputs { get; set; } = new List<string>();

      public List<string> AddressInputs { get; set; } = new List<string>();

      public List<MempoolOutput> Outputs { get; set; } = new List<MempoolOutput>();

      public List<MempoolInput> Inputs { get; set; } = new List<MempoolInput>();

      public string TransactionId { get; set; }
   }
}
