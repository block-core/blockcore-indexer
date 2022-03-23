using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Paging;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Cirrus.Controllers
{
   /// <summary>
   /// Query against the blockchain, allowing looking of blocks, transactions and addresses.
   /// </summary>
   [ApiController]
   [Route("api/query/cirrus")]
   public class CirrusQueryController : Controller
   {
      private readonly IPagingHelper paging;
      private readonly IStorage storage;
      private readonly ICirrusStorage cirrusMongoData;
      readonly IDAOContractAggregator DaoContractAggregator;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public CirrusQueryController(IPagingHelper paging, IStorage storage, IDAOContractAggregator daoContractAggregator)
      {
         this.paging = paging;
         this.storage = storage;
         DaoContractAggregator = daoContractAggregator;
         cirrusMongoData = storage as CirrusMongoData;
      }

      [HttpGet]
      [Route("contract/{address}")]
      public IActionResult GetAddressContract([MinLength(30)][MaxLength(100)] string address)
      {
         return Ok(cirrusMongoData.ContractCreate(address));
      }

      [HttpGet]
      [Route("contract/{address}/transactions")]
      public IActionResult GetAddressCall([MinLength(30)][MaxLength(100)] string address, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.ContractCall(address, null, offset, limit));
      }

      [HttpGet]
      [Route("contract/{address}/transactions/{filterAddress}")]
      public IActionResult GetAddressCallFilter([MinLength(30)][MaxLength(100)] string address, [MinLength(30)][MaxLength(100)] string filterAddress, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.ContractCall(address, filterAddress, offset, limit));
      }

      [HttpGet]
      [Route("contract/transaction/{transactionid}")]
      public IActionResult GetTransactionContract([MinLength(30)][MaxLength(100)] string transactionid)
      {
         return Ok(cirrusMongoData.ContractTransaction(transactionid));
      }

      [HttpGet]
      [Route("contract/code/{address}")]
      public IActionResult GetContractCode([MinLength(30)][MaxLength(100)] string address)
      {
         return Ok(cirrusMongoData.ContractCode(address));
      }

      [HttpGet]
      [Route("contract/dao/{address}")]
      [SlowRequestsFilteerAttribute]
      public async Task<IActionResult> GetDaoContractByAddress([MinLength(30)][MaxLength(100)] string address)
      {
         var contract = await DaoContractAggregator.ComputeDaoContractForAddressAsync(address);

         if (contract is null)
         {
            return NotFound();
         }

         return Ok(contract);
      }

      private IActionResult OkPaging<T>(QueryResult<T> result)
      {
         paging.Write(HttpContext, result);

         if (result == null)
         {
            return NotFound();
         }

         if (HttpContext.Request.Query.ContainsKey("envelope"))
         {
            return Ok(result);
         }
         else
         {
            return Ok(result.Items);
         }
      }

      private IActionResult OkItem<T>(T result)
      {
         if (result == null)
         {
            return NotFound();
         }

         return Ok(result);
      }
   }
}
