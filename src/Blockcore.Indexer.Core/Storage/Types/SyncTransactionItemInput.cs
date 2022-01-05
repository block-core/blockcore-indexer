namespace Blockcore.Indexer.Core.Storage.Types
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

      public string InputAddress { get; set; }

      public long InputAmount { get; set; }

      public string InputType { get; set; }

      #endregion
   }
}
