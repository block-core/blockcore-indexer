using System.Linq;

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
      public List<MapTransactionBlock> MapTransactionBlocks { get; set; } = new List<MapTransactionBlock>();
      public Dictionary<long, MapBlock> MapBlocks { get; set; } = new Dictionary<long, MapBlock>();
      public List<MapTransaction> MapTransactions { get; set; } = new List<MapTransaction>();

      public List<AddressTransaction> AddressTransactions { get; set; } = new List<AddressTransaction>();

      public List<AddressForInput> AddressForInputs { get; set; } = new List<AddressForInput>();
   }
}
