using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Paging;
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
      private readonly ICirrusStorage cirrusMongoData;
      readonly IComputeSmartContractService<DaoContractTable> daoContractService;
      readonly IComputeSmartContractService<StandardTokenContractTable> standardTokenService;
      readonly IComputeSmartContractService<NonFungibleTokenContractTable> nonFungibleTokenService;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public CirrusQueryController(IPagingHelper paging,
         IComputeSmartContractService<DaoContractTable> daoContractAggregator, ICirrusStorage cirrusMongoData, IComputeSmartContractService<StandardTokenContractTable> standardTokenService, IComputeSmartContractService<NonFungibleTokenContractTable> nonFungibleTokenService)
      {
         this.paging = paging;
         daoContractService = daoContractAggregator;
         this.cirrusMongoData = cirrusMongoData;
         this.standardTokenService = standardTokenService;
         this.nonFungibleTokenService = nonFungibleTokenService;
      }

      [HttpGet]
      [Route("contract/list")]
      public IActionResult GetGroupedContracts()
      {
         return OkPaging(cirrusMongoData.GroupedContracts());
      }

      [HttpGet]
      [Route("contract/list/{contractType}")]
      public IActionResult GetContracts([MinLength(2)][MaxLength(100)] string contractType, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.ListContracts(contractType, offset, limit));
      }

      [HttpGet]
      [Route("collectables/{ownerAddress}")]
      public IActionResult GetAddressAssets([MinLength(30)][MaxLength(100)] string ownerAddress, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return Ok(cirrusMongoData.GetNonFungibleTokensForAddressAsync(ownerAddress,offset,limit).Result);
      }

      [HttpGet]
      [Route("tokens/{ownerAddress}")]
      public IActionResult GettokensForAddress([MinLength(30)][MaxLength(100)] string ownerAddress, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return Ok(cirrusMongoData.GetStandardTokensForAddressAsync(ownerAddress,offset,limit).Result);
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
         var contract = await cirrusMongoData.GetDaoContractByAddressAsync(address);

         if (contract is null)
         {
            return NotFound();
         }

         return Ok(contract);
      }

      [HttpGet]
      [Route("contract/standardtoken/{address}")]
      [SlowRequestsFilteerAttribute]
      public async Task<IActionResult> GetStandardTokenContractByAddress([MinLength(30)][MaxLength(100)] string address)
      {
         var contract = await cirrusMongoData.GetStandardTokenContractByAddressAsync(address);

         if (contract is null)
         {
            return NotFound();
         }

         return Ok(contract);
      }

      [HttpGet]
      [Route("contract/standardtoken/{address}/{filterAddress}")]
      [SlowRequestsFilteerAttribute]
      public async Task<IActionResult> GetStandardTokenContractByAddress([MinLength(30)][MaxLength(100)] string address, [MinLength(30)][MaxLength(100)] string filterAddress)
      {
         var contract = await cirrusMongoData.GetStandardTokenByIdAsync(address, filterAddress);

         if (contract is null)
         {
            return NotFound();
         }

         return Ok(contract);
      }

      [HttpGet]
      [Route("contract/nonfungibletoken/{address}")]
      [SlowRequestsFilteerAttribute]
      public async Task<IActionResult> GetNonFungibleTokenContractByAddress([MinLength(30)][MaxLength(100)] string address)
      {
         var contract = await cirrusMongoData.GetNonFungibleTokenContractByAddressAsync(address);

         if (contract is null)
         {
            return NotFound();
         }

         return Ok(contract);
      }

      [HttpGet]
      [Route("contract/nonfungibletoken/{address}/tokens/{id}")]
      [SlowRequestsFilteerAttribute]
      public async Task<IActionResult> GetNonFungibleTokenById([MinLength(30)][MaxLength(100)] string address,
         [MinLength(1)][MaxLength(100)] string id)
      {
         var token = await cirrusMongoData.GetNonFungibleTokenByIdAsync(address, id);

         if (token is null)
         {
            return NotFound();
         }

         return Ok(token);
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
