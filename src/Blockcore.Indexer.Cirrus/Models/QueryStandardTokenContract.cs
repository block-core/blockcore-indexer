using System.Collections.Generic;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

namespace Blockcore.Indexer.Cirrus.Models;

public class QueryStandardTokenContract
{
   public string Name { get; set; }

   public string Symbol { get; set; }

   public long TotalSupply { get; set; }

   public long Decimals { get; set; }
   public long CreatedOnBlock { get; set; }
   public string CreatorAddress { get; set; }
   public List<StandardTokenHolderTable> tokens { get; set; }
}
