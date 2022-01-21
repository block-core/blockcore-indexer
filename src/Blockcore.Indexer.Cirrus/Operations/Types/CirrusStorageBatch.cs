using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Operations.Types
{
   public class CirrusStorageBatch
   {
      public List<CirrusContractTable> CirrusContractTable { get; set; } = new List<CirrusContractTable>();

      public List<CirrusContractCodeTable> CirrusContractCodeTable { get; set; } = new List<CirrusContractCodeTable>();

   }
}
