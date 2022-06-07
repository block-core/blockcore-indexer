using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;

public class NonFungibleTokenSmartContractTransactionsLookup : ISmartContractTransactionsLookup<NonFungibleTokenComputedTable>
{
   readonly ICirrusMongoDb mongoDb;

   public NonFungibleTokenSmartContractTransactionsLookup(ICirrusMongoDb mongoDb)
   {
      this.mongoDb = mongoDb;
   }

   public async Task<List<CirrusContractTable>> GetTransactionsForSmartContractAsync(string address,
      long lastProcessedBlockHeight)
   {
      return await mongoDb.CirrusContractTable
         .AsQueryable()
         .Where(_ => (_.ToAddress == address ||
                      _.Logs.Any(_ => _.Log.Data.ContainsKey("contract") && _.Log.Data["contract"] == address)) &&
                     (_.Success && _.BlockIndex > lastProcessedBlockHeight))
         .ToListAsync();
   }
}
