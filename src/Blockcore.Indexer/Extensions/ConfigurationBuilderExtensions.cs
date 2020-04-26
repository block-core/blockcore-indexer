using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Blockcore
{
   // TODO: Move this class into Blockcore.Core. Until then, this must be kept in sync between Blockcore Indexer, Blockcore Explorer and Blockcore Insight.
   public static class ConfigurationBuilderExtensions
   {
      /// <summary>
      /// Takes the json stream and adds it to the beginning of source array.
      /// </summary>
      /// <param name="builder"></param>
      /// <param name="stream"></param>
      /// <returns></returns>
      public static IConfigurationBuilder AddJsonStreamFirstIndex(this IConfigurationBuilder builder, Stream stream)
      {
         int count = builder.Sources.Count;

         builder.AddJsonStream(stream);

         IList<IConfigurationSource> sources = builder.Sources;

         // First take an in-memory copy of our latest source.
         IConfigurationSource lastSource = sources[count];

         for (int i = count; i-- > 0;)
         {
            // Move the source one index up.
            sources[i + 1] = sources[i];
         }

         // Now we can add our last source to the first index.
         sources[0] = lastSource;

         return builder;
      }

      /// <summary>
      /// Use to enable dynamic loading of blockchain configurations from the official Blockcore Chains website.
      /// </summary>
      /// <param name="builder"></param>
      /// <param name="title"></param>
      /// <param name="args"></param>
      /// <returns></returns>
      public static IConfigurationBuilder AddBlockcore(this IConfigurationBuilder builder, string title, string[] args)
      {
         Blockcore.Properties.BlockcoreLogo.SetTitle(title);
         Console.WriteLine(Blockcore.Properties.BlockcoreLogo.Logo);

         string chain = args
            .DefaultIfEmpty("--chain=BTC")
            .Where(arg => arg.StartsWith("--chain", ignoreCase: true, CultureInfo.InvariantCulture))
            .Select(arg => arg.Replace("--chain=", string.Empty, ignoreCase: true, CultureInfo.InvariantCulture))
            .FirstOrDefault();

         if (string.IsNullOrWhiteSpace(chain))
         {
            throw new ArgumentNullException("--chain", "You must specify the --chain argument. It can be either chain name, or URL to a json configuration.");
         }

         Console.WriteLine("CHAIN: " + chain);
         string url = chain.Contains("/") ? chain : $"https://chains.blockcore.net/chains/{chain}.json";
         Console.WriteLine("SETUP: " + url);

         var http = new HttpClient();
         HttpResponseMessage result = http.GetAsync(url).Result;

         if (result.IsSuccessStatusCode)
         {
            builder.AddJsonStreamFirstIndex(result.Content.ReadAsStreamAsync().Result);
         }
         else
         {
            throw new ApplicationException("Unable to read the supplied configuration.");
         }

         return builder;
      }
   }
}
