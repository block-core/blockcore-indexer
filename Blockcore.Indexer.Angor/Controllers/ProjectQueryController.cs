using System.ComponentModel.DataAnnotations;
using Blockcore.Indexer.Angor.Storage;
using Blockcore.Indexer.Core.Paging;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Angor.Controllers;

[ApiController]
[Route("api/query/Angor")]
public class ProjectQueryController : Controller
{
   private readonly IPagingHelper paging;
   private readonly IAngorStorage angorStorage;

   public ProjectQueryController(IPagingHelper paging, IAngorStorage cirrusMongoData)
   {
      this.paging = paging;
      angorStorage = cirrusMongoData;
   }

   [HttpGet]
   [Route("projects/{projectId}")]
   public IActionResult GetContracts([MinLength(2)][MaxLength(100)] string projectId)
   {
      return Ok(angorStorage.GetProjectAsync(projectId));
   }

   [HttpGet]
   [Route("projects")]
   public async Task<IActionResult> GetProjects( [Range(0, long.MaxValue)] int? offset = 0, [Range(1, 50)] int limit = 10)
   {
      var projects = await angorStorage.GetProjectsAsync(offset, limit);

      return OkPaging(projects);
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
}
