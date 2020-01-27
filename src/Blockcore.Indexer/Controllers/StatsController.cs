namespace Blockcore.Indexer.Api.Handlers
{
   using System.Threading.Tasks;
   using Microsoft.AspNetCore.Mvc;

   /// <summary>
   /// Controller to get some information about a coin.
   /// </summary>
   [ApiController]
   [Route("api/stats")]
   public class StatsController : ControllerBase
   {
      private readonly StatsHandler statsHandler;

      /// <summary>
      /// Initializes a new instance of the <see cref="StatsController"/> class.
      /// </summary>
      public StatsController(StatsHandler statsHandler)
      {
         this.statsHandler = statsHandler;
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

      [HttpGet]
      [Route("peers")]
      public async Task<IActionResult> Peers()
      {
         System.Collections.Generic.List<Client.Types.PeerInfo> ret = await statsHandler.Peers();

         return new OkObjectResult(ret);
      }
   }
}
