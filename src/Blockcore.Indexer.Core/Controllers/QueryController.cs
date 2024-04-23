using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Paging;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Core.Controllers
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
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}")]
      public IActionResult GetAddress([MinLength(4)][MaxLength(100)] string address)
      {
         return Ok(storage.AddressBalance(address));
      }

      /// <summary>
      /// Only returns addresses with outputs or spent outputs (also when balance is 0)
      /// </summary>
      /// <param name="addresses"></param>
      /// <param name="includeUnconfirmed"></param>
      /// <returns></returns>
      [HttpPost]
      [Route("addresses/balance")]
      public IActionResult GetAddressesBalance(IList<string> addresses, bool? includeUnconfirmed)
      {
         return Ok(storage.QuickBalancesLookupForAddressesWithHistoryCheckAsync(addresses, includeUnconfirmed ?? false).Result);
      }

      /// <summary>
      /// Get transactions that exists on the address.
      /// </summary>
      /// <param name="address"></param>
      /// <param name="offset">Set to null to get latest entries and not the first entries.</param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("address/{address}/transactions")]
      public IActionResult GetAddressTransactions([MinLength(4)][MaxLength(100)] string address, [Range(0, int.MaxValue)] int? offset = null, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.AddressHistory(address, offset, limit));
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
      public async Task<IActionResult> GetAddressTransactionsUnspent([MinLength(30)][MaxLength(100)] string address, long confirmations = 0, [Range(0, int.MaxValue)] int offset = 0, [Range(1, 50)] int limit = 10)
      {
         QueryResult<Storage.Mongo.Types.OutputTable> result = await storage.GetUnspentTransactionsByAddressAsync(address, confirmations, offset, limit);

         return OkPaging(result);
      }

      /// <summary>
      /// Returns transactions in the memory pool (mempool).
      /// </summary>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("mempool/transactions")]
      public IActionResult GetMempoolTransactions([Range(0, int.MaxValue)] int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.GetMemoryTransactionsSlim(offset, limit));
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
      /// Get a transaction in hex format based on the transaction ID (hash).
      /// </summary>
      /// <param name="transactionId"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("transaction/{transactionId}/hex")]
      public IActionResult GetTransactionHex(string transactionId)
      {
         return OkItem(storage.GetRawTransaction(transactionId));
      }


      /// <summary>
      /// Returns blocks based on the offset and limit. The blocks are sorted from from lowest to highest index. You can use the "link" HTTP header to get dynamic paging links.
      /// </summary>
      /// <param name="offset">If value set to null, then query will start from block tip, not from 0 (genesis).</param>
      /// <param name="limit">Number of blocks to return. Maximum 50.</param>
      [HttpGet]
      [Route("block")]
      public IActionResult GetBlocks([Range(0, int.MaxValue)] int? offset = null, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.Blocks(offset, limit));
      }

      /// <summary>
      /// Return transactions in a block based on block hash.
      /// </summary>
      /// <param name="offset"></param>
      /// <param name="limit"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/{hash}/transactions")]
      public IActionResult GetBlockByHashTransactions(string hash, [Range(0, int.MaxValue)] int offset = 0, [Range(1, 50)] int limit = 10)
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
      /// Returns a block based on the block id (hash).
      /// </summary>
      /// <param name="hash">Hash (ID) of the block to return.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/{hash}/hex")]
      public IActionResult GetBlockHex(string hash)
      {
         return OkItem(storage.GetRawBlock(hash));
      }

      /// <summary>
      /// Returns orphan blocks based on the offset and limit. Orphan blocks are blocks that are not part of the main chain.
      /// </summary>
      /// <param name="offset">If value set to null, then query will start from block tip, not from 0 (genesis).</param>
      /// <param name="limit">Number of blocks to return. Maximum 50.</param>
      [HttpGet]
      [Route("block/orphan")]
      public IActionResult GetOrphanBlocks([Range(0, int.MaxValue)] int? offset = null, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.OrphanBlocks(offset, limit));
      }

      /// <summary>
      /// Returns info of an orphan block based on the block id (hash).
      /// </summary>
      /// <param name="hash">Hash (ID) of the block to return.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/orphan/{hash}")]
      public IActionResult GetOrphanBlockByHash(string hash)
      {
         return OkItem(storage.OrphanBlockByHash(hash));
      }

      /// <summary>
      /// Returns a block based on the block height (index).
      /// </summary>
      /// <param name="index">The block height to get block from.</param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/index/{index}")]
      public IActionResult GetBlockByIndex([Range(0, long.MaxValue)] long index)
      {
         return OkItem(storage.BlockByIndex(index));
      }

      /// <summary>
      /// Return transactions in a block based on block height (index).
      /// </summary>
      /// <param name="index">The block height to get block from.</param>
      /// <param name="offset"></param>
      /// <returns></returns>
      [HttpGet]
      [Route("block/index/{index}/transactions")]
      public IActionResult GetBlockByIndexTransactions([Range(0, long.MaxValue)] long index, [Range(0, int.MaxValue)] int offset = 0, [Range(1, 50)] int limit = 10)
      {
         return OkPaging(storage.TransactionsByBlock(index, offset, limit));
      }

      /// <summary>
      /// Returns the latest blocks that is available.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("block/latest")]
      public IActionResult GetLatestBlock()
      {
         return OkItem(storage.GetLatestBlock());
      }

      private IActionResult OkPaging<T>(QueryResult<T> result)
      {
         if (result == null)
         {
            return NotFound();
         }

         paging.Write(HttpContext, result);

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
