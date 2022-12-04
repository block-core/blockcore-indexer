using Blockcore.Indexer.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Indexer
{
   public class BlockcoreStartup
   {
      public IConfiguration Configuration { get; }

      public BlockcoreStartup(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      public void ConfigureServices(IServiceCollection services)
      {
         Startup.AddIndexerServices(services, Configuration);
      }

      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         Startup.Configure(app, env);
      }
   }
}
