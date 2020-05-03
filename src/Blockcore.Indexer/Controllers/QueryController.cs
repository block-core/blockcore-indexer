using System.Linq;

namespace Blockcore.Indexer.Api.Handlers
{
   using System;
   using System.Net;
   using Blockcore.Indexer.Paging;
   using Microsoft.AspNetCore.Mvc;
   using Swashbuckle.AspNetCore.Annotations;

   /// <summary>
   /// Controller to expose an api that queries the blockchain.
   /// </summary>
   [ApiController]
   [Route("api/query")]
   public class QueryController : Controller
   {
      private readonly QueryHandler handler;
      private readonly IPagingHelper paging;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public QueryController(QueryHandler queryHandler, IPagingHelper paging)
      {
         handler = queryHandler;
         this.paging = paging;
      }

      /// <summary>
      /// Get transactions that belong to the specified address.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/transactions")]
      public IActionResult GetAddressTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressTransactions(address, confirmations);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/transactions/{count}")]
      public IActionResult GetAddressTransactionsCount(string address, long confirmations, int count)
      {
         Types.QueryAddress ret = handler.GetAddressTransactions(address, confirmations);

         if (ret.Transactions.Count() > count)
         {
            ret.Transactions = ret.Transactions.Take(count);
         }

         if (ret.UnconfirmedTransactions.Count() > count)
         {
            ret.UnconfirmedTransactions = ret.UnconfirmedTransactions.Take(count);
         }

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}")]
      public IActionResult GetAddress(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddress(address, confirmations);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/unspent/transactions")]
      public IActionResult GetAddressUtxoTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoTransactions(address, confirmations);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/unspent")]
      public IActionResult GetAddressUtxo(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxo(address, confirmations);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/unspent/confirmed")]
      public IActionResult GetAddressUtxoConfirmedTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoConfirmedTransactions(address, confirmations);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long}/unspent/unconfirmed")]
      public IActionResult GetAddressUtxoUnconfirmedTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoUnconfirmedTransactions(address, confirmations);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/unspent")]
      public IActionResult GetAddressUtxo(string address)
      {
         Types.QueryAddress ret = handler.GetAddressUtxo(address, 0);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}")]
      public IActionResult GetAddress(string address)
      {
         Types.QueryAddress ret = handler.GetAddress(address, 0);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/unspent/transactions")]
      public IActionResult GetAddressUtxoTransactions(string address)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoTransactions(address, 0);

         return Ok(ret);
      }

      [HttpGet]
      [Route("address/{address}/transactions")]
      public IActionResult GetAddressTransactions(string address)
      {
         Types.QueryAddress ret = handler.GetAddressTransactions(address, 0);

         return Ok(ret);
      }

      /// <summary>
      /// Returns blocks based on the offset and limit. The blocks are sorted from from lowest to highest index. You can use the "link" HTTP header to get dynamic paging links.
      /// </summary>
      /// <remarks>
      /// Sample request:
      ///
      ///     POST /Todo
      ///     {
      ///        "id": 1,
      ///        "name": "Item1",
      ///        "isComplete": true
      ///     }
      ///
      /// </remarks>
      /// <param name="offset">If value set to 0, then query will start from block tip, not from 1 (genesis).</param>
      /// <param name="limit">Number of blocks to return. Maximum 50.</param>
      /// <returns>A newly created TodoItem</returns>
      /// <response code="201">Returns the newly created item</response>
      /// <response code="400">If the item is null</response>     
      [HttpGet]
      [Route("block")]
      public IActionResult GetBlocks(int offset = 0, int limit = 10)
      {
         if (limit > 50)
         {
            throw new ArgumentException("Limit is maximum 50.");
         }

         if (offset < 0)
         {
            throw new ArgumentException("Offset must be positive number.");
         }

         Types.QueryBlocks result = handler.BlockGetByLimitOffset(offset, limit);

         // If the offset is not set, we'll default to query for the last page. We must ensure our URLs reflect this.
         if (offset == 0)
         {
            offset = result.Total - limit + 1;
         }

         paging.Write(HttpContext, offset, limit, result.Total);

         return Ok(result.Blocks);
      }

      /// <summary>
      /// Returns a block based on the block id (hash).
      /// </summary>
      /// <param name="hash">Hash (ID) of the block to return.</param>
      /// <param name="transactions">Flag that determine if transactions should be included in the block. Defaults to false.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/{hash}")]
      public IActionResult GetBlockByHash(string hash, bool transactions = false)
      {
         Types.QueryBlock ret = handler.GetBlock(hash, transactions);

         if (ret == null)
         {
            return NotFound();
         }

         return Ok(ret);
      }

      /// <summary>
      /// Returns a block based on the block height (index).
      /// </summary>
      /// <param name="index">The block height to get block from.</param>
      /// <param name="transactions">Flag that determine if transactions should be included in the block. Defaults to false.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/index/{index}")]
      public IActionResult GetBlockByIndex(long index, bool transactions = false)
      {
         Types.QueryBlock ret = handler.GetBlock(index, transactions);

         if (ret == null)
         {
            return NotFound();
         }

         return Ok(ret);
      }

      /// <summary>
      /// Returns the latest blocks that is available.
      /// </summary>
      /// <param name="transactions"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/latest")]
      public IActionResult GetLatestBlock(bool transactions = false)
      {
         Types.QueryBlock ret = handler.GetLastBlock(transactions);

         if (ret == null)
         {
            return NotFound();
         }

         return Ok(ret);
      }

      [HttpGet]
      [Route("transaction/{transactionId}")]
      public IActionResult GetTransaction(string transactionId)
      {
         Types.QueryTransaction ret = handler.GetTransaction(transactionId);

         if (ret == null)
         {
            return NotFound();
         }

         return Ok(ret);
      }

      [HttpGet]
      [Route("mempool/transactions/{count}")]
      public IActionResult GetMempoolTransactions(int count)
      {
         Types.QueryMempoolTransactions ret = handler.GetMempoolTransactions(count);

         if (ret == null)
         {
            return NotFound();
         }

         return Ok(ret);
      }
   }
}
