using System;
using System.Collections.Generic;
using System.Text;

namespace Blockcore.Indexer.Storage.Types
{
   class SyncRichListInputs
   {
      public long value { get; set; }
      public string address { get; set; }

      public SyncRichListInputs(string address, long value)
      {
         this.address = address;
         this.value = value;
      }
   }
}
