using System;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types;

public class UnspentOutput
{
   public Guid _Id { get; set; }
   public UnspentOutput()
   {
      _Id = Guid.NewGuid();
   }
   public Outpoint Outpoint { get; set; }

   public string Address { get; set; }

   public long Value { get; set; }

   public uint BlockIndex { get; set; }
}
