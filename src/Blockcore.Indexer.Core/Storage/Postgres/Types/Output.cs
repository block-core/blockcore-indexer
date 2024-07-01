using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class Output : Storage.Types.Output
   {
      public virtual Transaction Transaction { get; set; }
   }
}
