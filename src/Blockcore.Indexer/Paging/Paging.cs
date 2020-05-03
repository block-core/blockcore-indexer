using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Blockcore.Indexer.Storage.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Blockcore.Indexer.Paging
{
   public class PagingLinkBuilder
   {
      private readonly HttpContext context;
      readonly Dictionary<string, StringValues> list;

      public PagingLinkBuilder(HttpContext context)
      {
         this.context = context;

         // Create the dictionary of query string values.
         list = QueryHelpers.ParseQuery(context.Request.QueryString.ToString());

      }

      public string Create(long offset, int limit, string rel)
      {
         // Make sure we update offset and limit
         list["offset"] = new StringValues(offset.ToString());
         list["limit"] = new StringValues(limit.ToString());

         // Get the relative URL before the query.
         var uri = new Uri(context.Request.Path, UriKind.Relative);

         // Create the query string from our updated values.
         var query = QueryString.Create(list);

         // Build the final URL and use .ToString() on QueryString to make sure it is encoded.
         string url = uri + query.ToString();

         return $"<{url}>; rel=\"{rel}\"";
      }
   }

   public interface IPagingHelper
   {
      void Write(HttpContext context, long offset, int limit, long total);

      void Write<T>(HttpContext context, QueryResult<T> queryResult);
   }

   /// <summary>
   /// Utility for paging support in ASP.NET API, with rendering of HTTP headers and navigation links.
   /// </summary>
   public class PagingHelper : IPagingHelper
   {
      public void Write<T>(HttpContext context, QueryResult<T> queryResult)
      {
         Write(context, queryResult.Offset, queryResult.Limit, queryResult.Total);
      }

      public void Write(HttpContext context, long offset, int limit, long total)
      {
         //if (offset < 1)
         //{
         //   throw new ArgumentException("Offset cannot be lower than 1.");
         //}

         // If there are no offset, we'll default to total acount minus limit.
         //if (offset == 0)
         //{
         //   offset = total - limit;
         //}

         PagingLinkBuilder builder = new PagingLinkBuilder(context);
         PathString route = context.Request.Path;
         List<string> links = new List<string>();

         // Determine total number of pages to get highest offset possible.
         //int pageCount = total > 0 ? (int)Math.Ceiling(total / (double)limit) : 0;

         links.Add(builder.Create(1, limit, "first"));
         links.Add(builder.Create((total - limit + 1), limit, "last")); // +1 to be correct on last.

         if (offset > 1)
         {
            links.Add(builder.Create((offset - limit), limit, "previous")); // +1 to be correct on previous.
         }

         if (offset + limit < total)
         {
            links.Add(builder.Create(offset + limit, limit, "next"));
         }

         context.Response.Headers["Access-Control-Allow-Headers"] = "*";
         context.Response.Headers["Access-Control-Allow-Origin"] = "*";
         context.Response.Headers["Access-Control-Expose-Headers"] = "*";
         //context.Response.Headers["Access-Control-Expose-Headers"] = "Content-Length,Link,Pagination-Returned,Pagination-Total";

         context.Response.Headers["Link"] = string.Join(", ", links);
         context.Response.Headers["Pagination-Offset"] = offset.ToString();
         context.Response.Headers["Pagination-Limit"] = limit.ToString();
         context.Response.Headers["Pagination-Total"] = total.ToString();
      }
   }
}
