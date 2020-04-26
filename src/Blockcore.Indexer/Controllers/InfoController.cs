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
   /// Controller to get some information about the node, network and consensus rules.
   /// </summary>
   [ApiController]
   [Route("api/info")]
   public class InfoController : ControllerBase
   {
      private readonly StatsHandler statsHandler;

      private readonly MongoData storage;

      /// <summary>
      /// Initializes a new instance of the <see cref="InfoController"/> class.
      /// </summary>
      public InfoController(StatsHandler statsHandler, IStorage storage)
      {
         this.statsHandler = statsHandler;
         this.storage = storage as MongoData;
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
