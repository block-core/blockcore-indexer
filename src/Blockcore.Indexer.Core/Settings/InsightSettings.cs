using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Models;

namespace Blockcore.Indexer.Settings
{
   public class InsightSettings
   {
      public InsightSettings()
      {
         Wallets = new List<Wallet>();
      }

      public List<Wallet> Wallets { get; set; }

      public List<RewardModel> Rewards { get; set; }
   }
}
