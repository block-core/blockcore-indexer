using System.Collections.Generic;
using Blockcore.Indexer.Core.Storage.Mongo.Types;

namespace Blockcore.Indexer.Core.Storage.Mongo;

public interface IMondoDbInfo
{
   public List<IndexView> GetIndexesBuildProgress();
}
