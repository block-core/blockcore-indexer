using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Client.Types;

public class LogResponse
{
   public string Address { get; set; }
   public string[] Topics { get; set; }
   public string Data { get; set; }

   public LogData Log { get; set; }
}

public class LogData
{
   public string Event { get; set; }

   public IDictionary<string, object> Data { get; set; }
}
