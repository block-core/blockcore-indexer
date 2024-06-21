using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Paging;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.NBitcoin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Core.Controllers
{
   /// <summary>
   /// Query against the blockchain, allowing looking of blocks, transactions and addresses.
   /// </summary>
   [ApiController]
   [Route("api/insight")]
   public class InsightController : Controller
   {
      private readonly IPagingHelper paging;
      private readonly IStorage storage;
      private readonly IMemoryCache cache;
      private readonly InsightSettings insightConfiguration;
      private readonly SyncConnection syncConnection;
      private readonly int unit = (int)MoneyUnit.BTC;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public InsightController(IPagingHelper paging, IStorage storage, IMemoryCache cache, IOptions<InsightSettings> insightConfiguration, SyncConnection connection)
      {
         this.paging = paging;
         this.storage = storage;
         this.cache = cache;
         this.insightConfiguration = insightConfiguration.Value;
         syncConnection = connection;
      }

      /// <summary>
      /// Returns all available information on the supply. The results is cached for 10 seconds.
      /// </summary>
      /// <returns></returns>
      [HttpGet("supply")]
      public ActionResult<Supply> GetSupply()
      {
         if (!cache.TryGetValue(CacheKeys.Supply, out Supply supply))
         {
            supply = CalculateSupply();

            // Cache the result for 10 seconds, just to improve performance if lots of queries.
            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

            // Save data in cache.
            cache.Set(CacheKeys.Supply, supply, cacheEntryOptions);
         }

         return Ok(supply);
      }

      /// <summary>
      /// Calculates the circulating supply that is available when funds, locked and burned wallets has been deducated.
      /// </summary>
      /// <returns></returns>
      [HttpGet("supply/circulating")]
      public ActionResult<decimal> GetCirculatingSupply()
      {
         return Ok(CalculateCirculatingSupply() / unit);
      }

      /// <summary>
      /// Returns the total supply available, including funds, locked and burnt wallets.
      /// </summary>
      /// <returns></returns>
      [HttpGet("supply/total")]
      public ActionResult<decimal> GetTotalSupply()
      {
         return Ok(storage.TotalBalance());
      }

      /// <summary>
      /// Returns an estimate of rewards that block producers have received.
      /// </summary>
      /// <returns></returns>
      [HttpGet("rewards")]
      public ActionResult<decimal> GetRewards()
      {
         long tip = storage.GetLatestBlock().BlockIndex;
         decimal rewards = CalculateRewards(tip);

         if (rewards == -1)
         {
            return Ok(rewards);
         }
         else
         {
            return Ok(CalculateRewards(tip) / unit);
         }
      }

      /// <summary>
      /// Retrieve details about known wallets. Results is cached for 10 seconds.
      /// </summary>
      /// <returns></returns>
      [HttpGet("wallets")]
      public IActionResult GetWallets()
      {
         if (!cache.TryGetValue(CacheKeys.Wallets, out List<Wallet> funds))
         {
            funds = RetrieveWallets();

            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

            // Save data in cache.
            cache.Set(CacheKeys.Wallets, funds, cacheEntryOptions);
         }

         return Ok(funds);
      }

      /// <summary>
      /// Returns richlist entries based on the offset and limit. The entries are sorted from from lowest to highest balance.
      /// </summary>
      [HttpGet("richlist")]
      public IActionResult GetRichlist([Range(0, int.MaxValue)] int offset = 0, [Range(1, 100)] int limit = 100)
      {
         return OkPaging(storage.Richlist(offset, limit));
      }

      private IActionResult OkPaging<T>(QueryResult<T> result)
      {
         paging.Write(HttpContext, result);

         if (result == null)
         {
            return NotFound();
         }

         if (HttpContext.Request.Query.ContainsKey("envelope"))
         {
            return Ok(result);
         }
         else
         {
            return Ok(result.Items);
         }
      }

      private Supply CalculateSupply()
      {
         long tip = storage.GetLatestBlock().BlockIndex;

         var supply = new Supply
         {
            Circulating = CalculateCirculatingSupply() / unit,
            Total = storage.TotalBalance() / unit,
            Max = syncConnection.Network.Consensus.MaxMoney / unit,
            Rewards = CalculateRewards(tip),
            Height = tip
         };

         return supply;
      }

      /// <summary>
      /// Returns a list of pre-configured wallets and their latest balances.
      /// </summary>
      /// <returns></returns>
      private List<Wallet> RetrieveWallets()
      {
         // TODO: Funds should be stored in the DB, and be editable by individual chains and not hard-coded.
         var funds = new List<Wallet>();

         List<Wallet> wallets = insightConfiguration.Wallets;

         foreach (Wallet wallet in wallets)
         {
            if (wallet.Address != null && wallet.Address.Length > 0)
            {
               var balances = storage.AddressBalances(wallet.Address);
               long balance = balances.Sum(b => b.Balance);
               wallet.Balance = balance;
            }

            funds.Add(wallet);
         }

         return funds;
      }

      private decimal CalculateCirculatingSupply()
      {
         long totalBalance = storage.TotalBalance();
         IEnumerable<string[]> addresses = insightConfiguration.Wallets.Where(w => !w.Circulating).Select(w => w.Address);

         List<string> listOfAddress = new List<string>();

         foreach (string[] list in addresses)
         {
            if (list != null)
            {
               listOfAddress.AddRange(list);
            }
         }

         var balances = storage.AddressBalances(listOfAddress);
         long walletBalances = balances.Sum(b => b.Balance);

         long circulatingSupply = totalBalance - walletBalances;

         return circulatingSupply;
      }

      private decimal CalculateRewards(long height)
      {
         if (insightConfiguration.Rewards == null || insightConfiguration.Rewards.Count == 0)
         {
            return -1;
         }

         // Remove all future reward heights.
         RewardModel[] filteredRewards = insightConfiguration.Rewards.Where(r => r.Height < height).ToArray();
         int filteredRewardsCount = filteredRewards.Length;

         long totalReward = 0;

         for (int i = 0; i < filteredRewardsCount; i++)
         {
            RewardModel reward = filteredRewards[i];

            long calculateTip;

            // This is the last configuration.
            if (i < (filteredRewardsCount - 1))
            {
               calculateTip = filteredRewards[i + 1].Height - reward.Height;
            }
            else
            {
               calculateTip = height;
            }

            long rewardCalculation = calculateTip * reward.Reward;
            totalReward += rewardCalculation;
         }

         return totalReward;
      }
   }
}
