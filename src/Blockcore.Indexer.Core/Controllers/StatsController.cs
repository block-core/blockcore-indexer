using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Handlers;
using Blockcore.Indexer.Core.Models;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Storage.Mongo;
using Microsoft.AspNetCore.Http;
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
      [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
      public IActionResult GetHeartbeat()
      {
         return Ok("Heartbeat");
      }

      [HttpGet]
      [Route("connections")]
      [ProducesResponseType(typeof(StatsConnection), StatusCodes.Status200OK)]
      public async Task<IActionResult> GetConnections()
      {
         StatsConnection ret = await statsHandler.StatsConnection();
         return Ok(ret);
      }

      [HttpGet()]
      [ProducesResponseType(typeof(Statistics), StatusCodes.Status200OK)]
      public async Task<IActionResult> GetStats()
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
      [ProducesResponseType(typeof(CoinInfo), StatusCodes.Status200OK)]
      public async Task<IActionResult> GetInfo()
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
      [ProducesResponseType(typeof(List<Client.Types.PeerInfo>), StatusCodes.Status200OK)]
      public async Task<IActionResult> GetCurrentPeers()
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
      [ProducesResponseType(typeof(List<Client.Types.PeerInfo>), StatusCodes.Status200OK)]
      public IActionResult GetPeersFromDate(DateTime date)
      {
         List<Client.Types.PeerInfo> list = storage.GetPeerFromDate(date);
         return Ok(list);
      }
   }
}
