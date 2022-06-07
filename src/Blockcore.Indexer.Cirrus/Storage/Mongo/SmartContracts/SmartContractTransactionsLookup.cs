using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public class SmartContractTransactionsLookup<T> : ISmartContractTransactionsLookup<T> where T : SmartContractComputedBase
{
   readonly ICirrusMongoDb mongoDb;

   public SmartContractTransactionsLookup(ICirrusMongoDb mongoDb)
   {
      this.mongoDb = mongoDb;
   }

   public async Task<List<CirrusContractTable>> GetTransactionsForSmartContractAsync(string address, long lastProcessedBlockHeight)
   {
      return await mongoDb.CirrusContractTable
         .AsQueryable()
         .Where(_ => _.ToAddress == address && _.Success && _.BlockIndex > lastProcessedBlockHeight)
         .ToListAsync();
      ;
   }
}
