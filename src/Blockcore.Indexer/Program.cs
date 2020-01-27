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
                  url = $"https://raw.githubusercontent.com/block-core/chaininfo/master/add-existing-setups/chains/{chain}.json";
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


      //public static void Main(string[] args)
      //{
      //    var chain = (args.Length == 0) ? string.Empty : args[0].ToUpper();

      //    chain = (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CHAIN"))) ? chain : Environment.GetEnvironmentVariable("CHAIN").ToUpper();

      //    if (string.IsNullOrWhiteSpace(chain))
      //    {
      //        throw new ArgumentNullException("CHAIN", "You must specify a single argument that indicates what chain to run. Either as an argument or ENV variable.");
      //    }

      //    var config = new ConfigurationBuilder()
      //      .SetBasePath(Directory.GetCurrentDirectory())
      //      .AddJsonFile("hosting.json", optional: true)
      //      .AddJsonFile("setup.json", optional: false, reloadOnChange: false)
      //      .AddJsonFile(Path.Combine("Setup", $"{chain}.json"), optional: true, reloadOnChange: false)
      //      .AddCommandLine(args)
      //      .AddEnvironmentVariables()
      //      .Build();

      //    WebHost.CreateDefaultBuilder(args)
      //       .UseConfiguration(config)
      //       .UseStartup<Startup>()
      //       .Build().Run();
      //}
   }
}
