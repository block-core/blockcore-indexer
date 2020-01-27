using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blockcore.Indexer
{
   public class CacheKeys
   {
      public static string BlockCount = "_BlockCount";

      public static TimeSpan BlockCountTime = TimeSpan.FromSeconds(10);
   }
}
