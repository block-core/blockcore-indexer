using System.ComponentModel.DataAnnotations;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class Input
   {

      public Outpoint outpoint { get; set; } 
      public string Address { get; set; }
      public long Value { get; set; }
      public string Txid { get; set; }
      public uint Vout { get; set; }
      public long BlockIndex { get; set; }
      public virtual Transaction Transaction { get; set; }
   }
}
