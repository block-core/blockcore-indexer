using System.Linq;

namespace Blockcore.Indexer.Api.Handlers
{
   using Microsoft.AspNetCore.Mvc;

   /// <summary>
   /// Controller to expose an api that queries the blockchain.
   /// </summary>
   [ApiController]
   [Route("api/query")]
   public class QueryController : Controller
   {
      private readonly QueryHandler handler;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryController"/> class.
      /// </summary>
      public QueryController(QueryHandler queryHandler)
      {
         handler = queryHandler;
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/transactions")]
      public IActionResult GetAddressTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressTransactions(address, confirmations);

         return new OkObjectResult(ret);
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

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}")]
      public IActionResult GetAddress(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddress(address, confirmations);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/unspent/transactions")]
      public IActionResult GetAddressUtxoTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoTransactions(address, confirmations);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/unspent")]
      public IActionResult GetAddressUtxo(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxo(address, confirmations);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long=0}/unspent/confirmed")]
      public IActionResult GetAddressUtxoConfirmedTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoConfirmedTransactions(address, confirmations);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/confirmations/{confirmations:long}/unspent/unconfirmed")]
      public IActionResult GetAddressUtxoUnconfirmedTransactions(string address, long confirmations)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoUnconfirmedTransactions(address, confirmations);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/unspent")]
      public IActionResult GetAddressUtxo(string address)
      {
         Types.QueryAddress ret = handler.GetAddressUtxo(address, 0);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}")]
      public IActionResult GetAddress(string address)
      {
         Types.QueryAddress ret = handler.GetAddress(address, 0);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/unspent/transactions")]
      public IActionResult GetAddressUtxoTransactions(string address)
      {
         Types.QueryAddress ret = handler.GetAddressUtxoTransactions(address, 0);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("address/{address}/transactions")]
      public IActionResult GetAddressTransactions(string address)
      {
         Types.QueryAddress ret = handler.GetAddressTransactions(address, 0);

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("block/latest/{transactions?}")]
      public IActionResult GetBlock(string transactions = null)
      {
         Types.QueryBlock ret = handler.GetLastBlock(!string.IsNullOrEmpty(transactions));

         if (ret == null)
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("block/{blockHash}/{transactions?}")]
      public IActionResult GetBlockByHash(string blockHash, string transactions = null)
      {
         Types.QueryBlock ret = handler.GetBlock(blockHash, !string.IsNullOrEmpty(transactions));

         if (ret == null)
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("block/index/{blockIndex}/{transactions?}")]
      public IActionResult GetBlockByHash(long blockIndex, string transactions = null)
      {
         Types.QueryBlock ret = handler.GetBlock(blockIndex, !string.IsNullOrEmpty(transactions));

         if (ret == null)
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("block/index/{blockIndex}/Count/{count}")]
      public IActionResult GetBlocksCount(long blockIndex, int count)
      {
         // Note - if blockIndex == -1 use latest block

         Types.QueryBlocks ret = handler.GetBlocks(blockIndex, count);

         if (ret == null)
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(ret);
      }


      [HttpGet]
      [Route("transaction/{transactionId}")]
      public IActionResult GetTransaction(string transactionId)
      {
         Types.QueryTransaction ret = handler.GetTransaction(transactionId);

         if (ret == null)
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("mempool/transactions/{count}")]
      public IActionResult GetMempoolTransactions(int count)
      {
         Types.QueryMempoolTransactions ret = handler.GetMempoolTransactions(count);

         if (ret == null)
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(ret);
      }
   }
}
