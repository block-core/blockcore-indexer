using System;
using System.ComponentModel.DataAnnotations;

namespace Blockcore.Indexer.Core.Storage.Types;

public class Input
{
   public Outpoint Outpoint { get; set; }

   public string Address { get; set; }

   public long Value { get; set; }

   public string TrxHash { get; set; }

   public uint BlockIndex { get; set; }
}
