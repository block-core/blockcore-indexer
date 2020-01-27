namespace Blockcore.Indexer.Storage.Types
{
   public class SyncTransactionItemInput
   {
      #region Public Properties

      public string InputCoinBase { get; set; }

      public int PreviousIndex { get; set; }

      public string PreviousTransactionHash { get; set; }

      public string ScriptSig { get; set; }

      public string WitScript { get; set; }

      public string SequenceLock { get; set; }

      #endregion
   }
}