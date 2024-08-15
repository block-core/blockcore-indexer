using System.ComponentModel.DataAnnotations;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class RichListEntry
   {
      [Key]
      public string Address { get; set; }
      public long Balance { get; set; }
   }
}
