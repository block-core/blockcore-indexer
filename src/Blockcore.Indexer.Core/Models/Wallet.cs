namespace Blockcore.Indexer.Core.Models
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

      /// <summary>
      /// Returns false if the Type is set to either Fund, Locked or Burn. Returns true for other type of wallets.
      /// </summary>
      public bool Circulating
      {
         get
         {
            return !(Type == "Fund" || Type == "Locked" || Type == "Burn");
         }
      }
   }
}
