namespace Blockcore.Indexer
{
   using System;
   using System.Net.Http;
   using Microsoft.AspNetCore.Hosting;
   using Microsoft.Extensions.Configuration;
   using Microsoft.Extensions.Hosting;
   using System.Linq;
   using System.Linq.Expressions;
   using System.Globalization;

   /// <summary>
   /// The application program.
   /// </summary>
   public class Program
   {
      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
         .ConfigureAppConfiguration(config =>
            {
               string chain = args
                  .DefaultIfEmpty("--chain=BTC")
                  .Where(arg => arg.StartsWith("--chain", ignoreCase: true, CultureInfo.InvariantCulture))
                  .Select(arg => arg.Replace("--chain=", string.Empty, ignoreCase: true, CultureInfo.InvariantCulture))
                  .FirstOrDefault();

               if (string.IsNullOrWhiteSpace(chain))
               {
                  throw new ArgumentNullException("--chain", "You must specify the --chain argument. It can be either chain name, or URL to a json configuration.");
               }

               string url;

               if (chain.Contains("/"))
               {
                  url = chain;
               }
               else
               {
                  // TODO: Update this to master branch when it is updated.
                  url = $"https://raw.githubusercontent.com/block-core/chaininfo/feature/add-existing-setups/chains/{chain}.json";
               }

               var http = new HttpClient();
               HttpResponseMessage result = http.GetAsync(url).Result;

               if (result.IsSuccessStatusCode)
               {
                  System.IO.Stream stream = result.Content.ReadAsStreamAsync().Result;
                  config.AddJsonStream(stream);
               }
               else
               {
                  throw new ApplicationException("Unable to read the supplied configuration.");
               }
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
               webBuilder.UseStartup<Startup>();
            });
   }
}
