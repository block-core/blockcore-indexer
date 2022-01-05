using System.Collections.Generic;
using Blockcore.Indexer.Core.Models;

namespace Blockcore.Indexer.Core.Settings
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
