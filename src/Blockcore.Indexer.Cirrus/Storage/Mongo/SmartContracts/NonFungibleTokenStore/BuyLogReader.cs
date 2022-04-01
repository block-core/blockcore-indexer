using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.NonFungibleTokenStore;

public class BuyLogReader : ILogReader<NonFungibleTokenComputedTable>
{
   public bool CanReadLogForMethodType(string methodType) => methodType.Equals("Buy");

   public bool IsTransactionLogComplete(LogResponse[] logs) => logs is { Length: 2 };

   public void UpdateContractFromTransactionLog(CirrusContractTable contractTransaction,
      NonFungibleTokenComputedTable computedTable)
   {
      var transferLog = contractTransaction.Logs[0];
      var tokenPurchaseLog = contractTransaction.Logs[1];

      string seller = (string)tokenPurchaseLog.Log.Data["seller"];

      string tokenId = (string)transferLog.Log.Data["tokenId"];

      var token = computedTable.Tokens.Single(_ => _.Id == tokenId);

      token.Owner = (string)tokenPurchaseLog.Log.Data["buyer"];

      var onSale = (OnSale)token.SalesHistory.Last(_ => _ is OnSale sale
                                                        && sale.Seller == seller
                                                        && !sale.Sold);

      onSale.Buyer = (string)tokenPurchaseLog.Log.Data["buyer"];
      onSale.Sold = true;
      onSale.PurchaseTransactionId = contractTransaction.TransactionId;
   }
}
