using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Blockcore.Indexer.Models
{
   public class Supply
   {
      public decimal Circulating { get; set; }

      public decimal Total { get; set; }

      public decimal Max { get; set; }

      public decimal Rewards { get; set; }

      public long Height { get; set; }
   }
}
