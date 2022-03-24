
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Binding;
using Blockcore.Indexer.Core.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Core.Controllers
{
   /// <summary>
   /// Controller to get some information about a coin.
   /// </summary>
   [ApiController]
   [Route("api/command")]
   public class CommandController : Controller
   {
      private readonly CommandHandler commandHandler;

      /// <summary>
      /// Initializes a new instance of the <see cref="CommandController"/> class.
      /// </summary>
      public CommandController(CommandHandler commandHandler)
      {
         this.commandHandler = commandHandler;
      }

      [HttpPost("send")]
      public async Task<IActionResult> Send([FromBody][ModelBinder(BinderType = typeof(RawStringModelBinder))] string data)
      {
         if (string.IsNullOrEmpty(data))
         {
            // http://stackoverflow.com/questions/9454811/which-http-status-code-to-use-for-required-parameters-not-provided
            return new StatusCodeResult(422);
         }

         string trx = data;


         // todo: add throttling on this endpoint.

         string ret = await commandHandler.SendTransaction(trx);
         return new OkObjectResult(ret);
      }
   }
}
