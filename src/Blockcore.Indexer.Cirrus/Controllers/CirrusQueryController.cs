using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Cirrus.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Operations;
using Blockcore.Indexer.Core.Paging;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.AspNetCore.Http;
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

      public CirrusQueryController(IPagingHelper paging, ICirrusStorage cirrusMongoData)
      {
         this.paging = paging;
         this.cirrusMongoData = cirrusMongoData;
      }

      [HttpGet]
      [Route("contract/list")]
      [ProducesResponseType(typeof(QueryResult<QueryContractGroup>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetGroupedContracts()
      {
         return OkPaging(cirrusMongoData.GroupedContracts());
      }

      [HttpGet]
      [Route("contract/list/{contractType}")]
      [ProducesResponseType(typeof(QueryResult<QueryContractList>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetContractsOfType([MinLength(2)][MaxLength(100)] string contractType, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.ListContracts(contractType, offset, limit));
      }

      [HttpGet]
      [Route("contracts/logs")]
      [ProducesResponseType(typeof(QueryResult<QueryBlockSmartContractsLogs>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> GetContractLogs([Range(0, long.MaxValue)] long startBlock,[Range(0, long.MaxValue)] long endBlock, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 1000)] int limit = 1000)
      {
         if (endBlock < startBlock)
            return BadRequest();

         return OkPaging(await cirrusMongoData.ListContractLogsAsync(startBlock,endBlock, offset, limit));
      }

      [HttpGet]
      [Route("collectables/{ownerAddress}")]
      [ProducesResponseType(typeof(QueryResult<QueryAddressAsset>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetNonFungibleTokensOwnedByAddress([MinLength(30)][MaxLength(100)] string ownerAddress, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.GetNonFungibleTokensForAddressAsync(ownerAddress,offset,limit).Result);
      }

      [HttpGet]
      [Route("tokens/{ownerAddress}")]
      [ProducesResponseType(typeof(QueryResult<QueryStandardToken>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetStandardTokensOwnedByAddress([MinLength(30)][MaxLength(100)] string ownerAddress, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.GetStandardTokensForAddressAsync(ownerAddress,offset,limit).Result);
      }

      [HttpGet]
      [Route("contract/{address}")]
      [ProducesResponseType(typeof(QueryContractCreate), StatusCodes.Status200OK)]
      public IActionResult GetSmartContractCreateTransaction([MinLength(30)][MaxLength(100)] string address)
      {
         return Ok(cirrusMongoData.ContractCreate(address));
      }

      [HttpGet]
      [Route("contract/{address}/transactions")]
      [ProducesResponseType(typeof(QueryResult<QueryContractCall>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetSmartContractCallTransactions([MinLength(30)][MaxLength(100)] string address, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.ContractCall(address, null, offset, limit));
      }

      [HttpGet]
      [Route("contract/{address}/transactions/{filterAddress}")]
      [ProducesResponseType(typeof(QueryResult<QueryContractCall>), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetSmartContractCallTransactionsBySender([MinLength(30)][MaxLength(100)] string address, [MinLength(30)][MaxLength(100)] string filterAddress, [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(cirrusMongoData.ContractCall(address, filterAddress, offset, limit));
      }

      [HttpGet]
      [Route("contract/transaction/{transactionid}")]
      [ProducesResponseType(typeof(QueryContractTransaction), StatusCodes.Status200OK)]
      public IActionResult GetSmartContractTransactionById([MinLength(30)][MaxLength(100)] string transactionid)
      {
         return Ok(cirrusMongoData.ContractTransaction(transactionid));
      }

      [HttpGet]
      [Route("contract/code/{address}")]
      [ProducesResponseType(typeof(QueryContractCode), StatusCodes.Status200OK)]
      public IActionResult GetSmartContractCodeByAddress([MinLength(30)][MaxLength(100)] string address)
      {
         return Ok(cirrusMongoData.ContractCode(address));
      }

      [HttpGet]
      [Route("contract/dao/{address}")]
      [SlowRequestsFilteerAttribute]
      [ProducesResponseType(typeof(QueryDAOContract), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
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
      [ProducesResponseType(typeof(QueryStandardTokenContract), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
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
      [ProducesResponseType(typeof(QueryStandardToken), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public async Task<IActionResult> GetStandardTokenContractByAddressFiltered([MinLength(30)][MaxLength(100)] string address, [MinLength(30)][MaxLength(100)] string filterAddress)
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
      [ProducesResponseType(typeof(QueryNonFungibleTokenContract), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
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
      [ProducesResponseType(typeof(NonFungibleTokenTable), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
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
