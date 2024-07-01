using System.ComponentModel.DataAnnotations;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class Input : Storage.Types.Input
   {
      public virtual Transaction Transaction { get; set; }
   }
}
