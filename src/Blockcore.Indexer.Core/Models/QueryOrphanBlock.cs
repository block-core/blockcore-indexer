using System;
using System.Collections.Generic;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Core.Models
{
   public class QueryOrphanBlock
   {
      public DateTime Created { get; set; }
      public uint BlockIndex { get; set; }
      public string BlockHash { get; set; }
      public BlockTable Block { get; set; }
   }
}
