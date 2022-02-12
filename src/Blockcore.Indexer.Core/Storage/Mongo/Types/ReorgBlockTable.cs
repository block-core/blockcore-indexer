using System;
using System.Collections.Generic;

namespace Blockcore.Indexer.Core.Storage.Mongo.Types;

public class ReorgBlockTable
{
   public DateTime Created { get; set; }
   public uint BlockIndex { get; set; }
   public string BlockHash { get; set; }
   public BlockTable Block { get; set; }
   public List<InputTable> Inputs { get; set; }
   public List<OutputTable> Outputs { get; set; }
   public List<TransactionBlockTable> TransactionIds { get; set; }
}
