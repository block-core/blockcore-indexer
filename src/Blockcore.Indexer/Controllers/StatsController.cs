namespace Blockcore.Indexer.Api.Handlers
{
   using System;
   using System.Collections.Generic;
   using System.Threading.Tasks;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Storage;
   using Blockcore.Indexer.Storage.Mongo;
   using Microsoft.AspNetCore.Mvc;

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
         return new OkObjectResult("Heartbeat");
      }

      [HttpGet]
      [Route("connections")]
      public async Task<IActionResult> Connections()
      {
         Types.StatsConnection ret = await statsHandler.StatsConnection();

         return new OkObjectResult(ret);
      }

      [HttpGet()]
      public async Task<IActionResult> Get()
      {
         Types.Statistics ret = await statsHandler.Statistics();

         return new OkObjectResult(ret);
      }

      [HttpGet]
      [Route("info")]
      public async Task<IActionResult> Info()
      {
         Types.CoinInfo ret = await statsHandler.CoinInformation();

         return new OkObjectResult(ret);
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

         return new OkObjectResult(ret);
      }

      /// <summary>
      /// Returns a list of nodes observed after the date supplied in the URL.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [Route("peers/{date}")]
      public async Task<IActionResult> Peers(DateTime date)
      {
         List<Client.Types.PeerInfo> list = storage.GetPeerFromDate(date);
         return new OkObjectResult(list);
      }
   }
}
