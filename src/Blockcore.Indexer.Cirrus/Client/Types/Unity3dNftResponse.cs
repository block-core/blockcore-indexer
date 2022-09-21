using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Client.Types;

public class Unity3dNftResponse
{
   public Dictionary<string, List<int>> OwnedIDsByContractAddress { get; set; }
}
