using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Indexer.Cirrus.Sync.SyncTasks;

public class SmartContractSyncRunner : TaskRunner
{
   ICirrusStorage storage;

   public SmartContractSyncRunner(IOptions<IndexerSettings> configuration, ILogger logger, ICirrusStorage storage) : base(configuration, logger)
   {
      this.storage = storage;
   }

   public override Task<bool> OnExecute() => throw new System.NotImplementedException();


   private async Task<List<object>> GetSmartContracts()
   {
      return new List<object>();
   }
}
