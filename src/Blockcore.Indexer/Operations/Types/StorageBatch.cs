namespace Blockcore.Indexer.Operations.Types
{
   #region Using Directives

   using System.Collections.Generic;
   using Blockcore.Indexer.Storage.Mongo.Types;

   #endregion Using Directives

   public class StorageBatch
   {
      public long TotalSize { get; set; }
      public Dictionary<string, MongoDB.Driver.WriteModel<MapTransactionAddress>> MapTransactionAddresses { get; set; } = new Dictionary<string, MongoDB.Driver.WriteModel<MapTransactionAddress>>();
      public List<Storage.Mongo.Types.MapTransactionBlock> MapTransactionBlocks { get; set; } = new List<Storage.Mongo.Types.MapTransactionBlock>();
      public List<Storage.Mongo.Types.MapBlock> MapBlocks { get; set; } = new List<Storage.Mongo.Types.MapBlock>();
      public List<Storage.Mongo.Types.MapTransaction> MapTransactions { get; set; } = new List<Storage.Mongo.Types.MapTransaction>();

      public void Clear()
      {
         TotalSize = 0;
         MapBlocks.Clear();
         MapTransactionBlocks.Clear();
         MapTransactionAddresses.Clear();
         MapTransactions.Clear();
      }
   }
}
