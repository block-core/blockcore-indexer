using System;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class Output : Storage.Types.Output
   {
      public Guid _Id { get; set; }
      public Output(){
         _Id = Guid.NewGuid();
      }
      public virtual Transaction Transaction { get; set; }
   }
}
