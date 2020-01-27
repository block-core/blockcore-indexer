namespace Blockcore.Indexer.Storage.Mongo.Types
{
   public class MapTransaction
   {
      #region Public Properties

      public byte[] RawTransaction { get; set; }

      public string TransactionId { get; set; }

      #endregion
   }
}