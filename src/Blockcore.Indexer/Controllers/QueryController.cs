using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Blockcore.Indexer.Paging;
using Blockcore.Indexer.Storage;
using Blockcore.Indexer.Storage.Mongo;
using Blockcore.Indexer.Storage.Types;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Api.Handlers
{
   /// <summary>
   /// Query against the blockchain, allowing looking of blocks, transactions and addresses.
   /// </summary>
   [ApiController]
   [Route("api/query")]
   public class QueryController : Controller
   {
      private readonly IPagingHelper paging;
      private readonly IStorage storage;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public QueryController(IPagingHelper paging, IStorage storage)
      {
         this.paging = paging;
         this.storage = storage;
      }

      /// <summary>
      /// Get the balance on address.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}")]
      public IActionResult GetAddress([MinLength(30)][MaxLength(54)]string address, long confirmations = 0)
      {
         return Ok(storage.AddressBalance(address, confirmations));
      }

      /// <summary>
      /// Get transactions that exists on the address.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}/transactions")]
      public IActionResult GetAddressTransactions([MinLength(30)][MaxLength(54)]string address, long confirmations = 0, [Range(0, long.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.AddressTransactions(address, confirmations, false, TransactionUsedFilter.All, offset, limit));
      }

      /// <summary>
      /// Get unconfirmed transactions that exists on the address, based on the confirmation value specified. Confirmations must be 1 or higher, as 0 will always return empty results.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}/transactions/unconfirmed")]
      public IActionResult GetAddressTransactionsUnconfirmed([MinLength(30)][MaxLength(54)]string address, [Range(1, long.MaxValue)]long confirmations, [MinLength(0)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.AddressTransactions(address, confirmations, true, TransactionUsedFilter.All, offset, limit));
      }

      /// <summary>
      /// Get spent transactions that exists on the address.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}/transactions/spent")]
      public IActionResult GetAddressTransactionsSpent([MinLength(30)][MaxLength(54)]string address, long confirmations = 0, [Range(0, long.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.AddressTransactions(address, confirmations, true, TransactionUsedFilter.Spent, offset, limit));
      }

      /// <summary>
      /// Get unspent transactions that exists on the address.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="confirmations"></param>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}/transactions/unspent")]
      public IActionResult GetAddressTransactionsUnspent([MinLength(30)][MaxLength(54)]string address, long confirmations = 0, [Range(0, long.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.AddressTransactions(address, confirmations, true, TransactionUsedFilter.Unspent, offset, limit));
      }

      /// <summary>
      /// Returns transactions in the memory pool (mempool).
      /// </summary>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("mempool/transactions")]
      public IActionResult GetMempoolTransactions([Range(0, int.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.GetMemoryTransactions(offset, limit));
      }

      /// <summary>
      /// Get the number of transactions in mempool.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("mempool/transactions/count")]
      public IActionResult GetMempoolTransactionsCount()
      {
         return OkItem(storage.GetMemoryTransactionsCount());
      }

      /// <summary>
      /// Get a transaction based on the transaction ID (hash).
      /// </summary>
      /// <param name="transactionId"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("transaction/{transactionId}")]
      public IActionResult GetTransaction(string transactionId)
      {
         return OkItem(storage.GetTransaction(transactionId));
      }

      /// <summary>
      /// Returns blocks based on the offset and limit. The blocks are sorted from from lowest to highest index. You can use the "link" HTTP header to get dynamic paging links.
      /// </summary>
      /// <param name="offset">If value set to 0, then query will start from block tip, not from 1 (genesis).</param>
      /// <param name="limit">Number of blocks to return. Maximum 50.</param>
      [HttpGet]
      [Route("block")]
      public IActionResult GetBlocks([Range(0, int.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.Blocks(offset, limit));
      }

       /// <summary>
       /// Return transactions in a block based on block hash.
       /// </summary>
       /// <param name="address"></param>
       /// <param name="confirmations"></param>
       /// <param name="offset"></param>
       /// <param name="limit"></param>
       /// <returns></returns>
      [HttpGet]
      [Route("block/{hash}/transactions")]
      public IActionResult GetBlockByHashTransactions(string hash, [Range(0, long.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.TransactionsByBlock(hash, offset, limit));
      }

      /// <summary>
      /// Returns a block based on the block id (hash).
      /// </summary>
      /// <param name="hash">Hash (ID) of the block to return.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/{hash}")]
      public IActionResult GetBlockByHash(string hash)
      {
         return OkItem(storage.BlockByHash(hash));
      }

      /// <summary>
      /// Returns a block based on the block height (index).
      /// </summary>
      /// <param name="index">The block height to get block from.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/index/{index}")]
      public IActionResult GetBlockByIndex([Range(0, long.MaxValue)]long index)
      {
         return OkItem(storage.BlockByIndex(index));
      }
      /// <summary>
      /// Return transactions in a block based on block height (index).
      /// </summary>
      /// <param name="index">The block height to get block from.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/index/{index}/transactions")]
      public IActionResult GetBlockByIndexTransactions([Range(0, long.MaxValue)]long index, [Range(0, long.MaxValue)]int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.TransactionsByBlock(index, offset, limit));
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
         return OkItem(storage.GetLatestBlock());
      }

      /// <summary>
      /// Returns richlist entries based on the offset and limit. The entries are sorted from from lowest to highest balance.
      /// </summary>
      [HttpGet]
      [Route("richlist")]
      public IActionResult GetRichlist([Range(0, int.MaxValue)]int offset = 0, [Range(1, 100)] int limit = 100)
      {
         return OkPaging(storage.Richlist(offset, limit));
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

      // TODO: Future API additions to get spent and unspent.
      //[HttpGet]
      //[Route("address/{address}/transactions/unspent")]
      //public IActionResult GetAddressTransactionsUnspent(string address, int offset = 0, int limit = 10, long confirmations = 0)
      //{
      //   var result = storage.AddressTransactions(address, confirmations, true, offset, limit);
      //   return Ok(result);
      //}

      //[HttpGet]
      //[Route("address/{address}/transactions/spent")]
      //public IActionResult GetAddressTransactionsSpent(string address, int offset = 0, int limit = 10, long confirmations = 0)
      //{
      //   var result = storage.AddressTransactions(address, confirmations, false, offset, limit);
      //   return Ok(result);
      //}
   }
}
