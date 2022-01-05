using Blockcore.Indexer.Core.Client;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Core.Controllers
{
   [ApiController]
   [ApiExplorerSettings(IgnoreApi = true)]
   public class ErrorController : ControllerBase
   {
      [Route("/error")]
      public IActionResult Error([FromServices] IWebHostEnvironment webHostEnvironment)
      {
         IExceptionHandlerFeature context = HttpContext.Features.Get<IExceptionHandlerFeature>();
         bool devMode = webHostEnvironment.EnvironmentName == "Development";

         if (context.Error is BitcoinClientException ex)
         {
            return Problem(
                title: ex.ErrorMessage,
                detail: devMode ? context.Error.StackTrace : null,
                statusCode: (int)ex.StatusCode);
         }
         else
         {
            return Problem(
                title: context.Error.Message,
                detail: context.Error.StackTrace,
                statusCode: HttpContext.Response.StatusCode);
         }
      }
   }
}
