using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blockcore.Indexer.Models
{
   public class Wallet
   {
      public string Name { get; set; }

      public string[] Address { get; set; }

      public string Type { get; set; }

      public string Url { get; set; }

      public string Logo { get; set; }

      public decimal InitialAmount { get; set; }

      public decimal Balance { get; set; }
   }
}
