using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Storage.Mongo;
using Blockcore.Indexer.Core.Controllers;
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
      private readonly CirrusMongoData cirrusMongoData;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public CirrusQueryController(IPagingHelper paging, IStorage storage)
      {
         this.paging = paging;
         this.storage = storage;
         cirrusMongoData = storage as CirrusMongoData;
      }

      [HttpGet]
      [Route("address-contract/{address}")]
      public IActionResult GetAddressContract([MinLength(30)][MaxLength(100)] string address)
      {
         return Ok(cirrusMongoData.AddressContract(address));
      }

      [HttpGet]
      [Route("transaction-contract/{transactionid}")]
      public IActionResult GetTransactionContract([MinLength(30)][MaxLength(100)] string transactionid)
      {
         return Ok(cirrusMongoData.TransactionContract(transactionid));
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
