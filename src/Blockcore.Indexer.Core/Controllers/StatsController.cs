using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Handlers;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Core.Controllers
{
   /// <summary>
   /// Controller to get some information about a coin.
   /// </summary>
   [ApiController]
   [Route("api/stats")]
   public class StatsController : ControllerBase
   {
      private readonly StatsHandler statsHandler;

      private readonly MongoData storage;

      /// <summary>
      /// Initializes a new instance of the <see cref="StatsController"/> class.
      /// </summary>
      public StatsController(StatsHandler statsHandler, IStorage storage)
      {
         this.statsHandler = statsHandler;
         this.storage = storage as MongoData;
      }

      [HttpGet]
      [Route("heartbeat")]
      public IActionResult Heartbeat()
      {
         return Ok("Heartbeat");
      }

      [HttpGet]
      [Route("connections")]
      public async Task<IActionResult> Connections()
      {
         StatsConnection ret = await statsHandler.StatsConnection();
         return Ok(ret);
      }

      [HttpGet()]
      public async Task<IActionResult> Get()
      {
         Statistics ret = await statsHandler.Statistics();
         return Ok(ret);
      }

      /// <summary>
      /// Returns a lot of information about the network, node and consensus rules.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("info")]
      public async Task<IActionResult> Info()
      {
         CoinInfo ret = await statsHandler.CoinInformation();
         return Ok(ret);
      }

      /// <summary>
      /// Returns a list of currently connected nodes.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("peers")]
      public async Task<IActionResult> Peers()
      {
         System.Collections.Generic.List<Client.Types.PeerInfo> ret = await statsHandler.Peers();
         return Ok(ret);
      }

      /// <summary>
      /// Returns a list of nodes observed after the date supplied in the URL.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("peers/{date}")]
      public IActionResult Peers(DateTime date)
      {
         List<Client.Types.PeerInfo> list = storage.GetPeerFromDate(date);
         return Ok(list);
      }

      /// <summary>
      /// Returns fee rate estimations for each of the confirmations in the array.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("fee")]
      public async Task<IActionResult> FeeEstimation([FromQuery] int[] confirmations)
      {
         var res = await statsHandler.GetFeeEstimation(confirmations);

         return Ok(res);
      }
   }
}
