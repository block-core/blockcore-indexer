using System;
using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types;

public class ReorgBlockTable
{
   public DateTime Created { get; set; }
   public uint BlockIndex { get; set; }
   public string BlockHash { get; set; }
   public Block Block { get; set; }

}
