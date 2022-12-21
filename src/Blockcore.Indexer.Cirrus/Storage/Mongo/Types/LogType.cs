namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public enum LogType
{
   TransferLog,
   TokenSaleCanceledLog,
   TokenPurchasedLog,
   RoyaltyPaidLog,
   AuctionStartedLog,
   HighestBidUpdatedLog,
   AuctionEndFailedLog,
   AuctionEndSucceedLog,
   TokenOnSaleLog,
   MintExtract,
   ProposalAddedLog,
   ProposalVotedLog,
   ProposalExecutedLog,
   FundRaisedLog
}
