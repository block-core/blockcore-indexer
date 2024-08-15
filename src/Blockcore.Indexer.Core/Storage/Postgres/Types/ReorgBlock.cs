using System;
using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types;


//TODO => reorgBlock is not implemented correctly, to be fixed
public class ReorgBlock
{
   public DateTime Created { get; set; }
   public uint BlockIndex { get; set; }
   public string BlockHash { get; set; }
   public Block Block { get; set; }
}
