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
   public delegate string RequestContextDelegate();

   public class PageLink
   {
      public int Limit { get; set; }

      public long Offset { get; set; }

      public string Rel { get; set; }

      public string Url { get; set; }
   }

   public class PagingLinkBuilder
   {
      private readonly RequestContextDelegate requestPath;
      private readonly RequestContextDelegate requestQuery;
      readonly Dictionary<string, StringValues> list;

      public PagingLinkBuilder(RequestContextDelegate requestPath, RequestContextDelegate requestQuery)
      {
         this.requestPath = requestPath;

         // Create the dictionary of query string values.
         list = QueryHelpers.ParseQuery(requestQuery());
         //list = QueryHelpers.ParseQuery(context.Request.QueryString.ToString());
         // context.Request.Path
      }

      public PageLink Create(long offset, int limit, string rel)
      {
         PageLink link = new PageLink();

         link.Offset = offset;
         link.Limit = limit;
         link.Rel = rel;

         // Make sure we update offset and limit
         list["offset"] = new StringValues(offset.ToString());
         list["limit"] = new StringValues(limit.ToString());

         // Get the relative URL before the query.
         var uri = new Uri(requestPath(), UriKind.Relative);

         // Create the query string from our updated values.
         var query = QueryString.Create(list);

         // Build the final URL and use .ToString() on QueryString to make sure it is encoded.
         string url = uri + query.ToString();

         link.Url = $"<{url}>; rel=\"{rel}\"";

         return link;
      }
      public List<PageLink> Links(long offset, int limit, long total)
      {
         List<PageLink> links = new List<PageLink>();

         /*
         SPECIFICATION:
         The paging goes from 1 to total. The lowest item is 1.
         Supplying offset 0, will return the last page of data.
         On the last page of data, the "next" link should not be returned.
         On the first page of data, the "previous" link should not be returned.
         "first" and "last" are always returned no matter what.
         If the limit is higher than total available items, handle this accordingly.
         The array of links is always ordered: "first", "last", "previous" and "next".
         */

         // If the limit is higher than total, make the limit the total amount that is available.
         if (limit > total)
         {
            limit = (int)total;
         }

         links.Add(Create(1, limit, "first"));
         links.Add(Create((total - limit + 1), limit, "last"));

         // If the limit is equal total, we won't be rendering next/previous links.
         if (limit == total)
         {
            return links;
         }

         // if the offset is 0, we'll pick the last page.
         if (offset == 0)
         {
            offset = (total - limit + 1);
         }

         // If offset queried is higher than 1, we'll always include the previous link.
         if (offset > 1)
         {
            long offsetPrevious = offset - limit;

            // If offset previous is lower than 1, make sure it's 1.
            if (offsetPrevious < 1)
            {
               offsetPrevious = 1;
            }

            links.Add(Create(offsetPrevious, limit, "previous"));
         }

         long offsetNext = offset + limit;

         if (offset + limit < total)
         {
            // Make sure the offset next is never higher than total.
            if (offsetNext > total)
            {
               offsetNext = total;
            }

            if (offset == 0)
            {
               offsetNext = offsetNext++;
            }

            links.Add(Create(offsetNext, limit, "next"));
         }

         return links;
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
         RequestContextDelegate requestPath = delegate { return context.Request.Path; };
         RequestContextDelegate requestQuery = delegate { return context.Request.QueryString.ToString(); };

         PagingLinkBuilder builder = new PagingLinkBuilder(requestPath, requestQuery);
         List<PageLink> links = builder.Links(offset, limit, total);

         context.Response.Headers["Access-Control-Allow-Headers"] = "*";
         context.Response.Headers["Access-Control-Allow-Origin"] = "*";
         context.Response.Headers["Access-Control-Expose-Headers"] = "*";
         //context.Response.Headers["Access-Control-Expose-Headers"] = "Content-Length,Link,Pagination-Returned,Pagination-Total";

         context.Response.Headers["Link"] = string.Join(", ", links.Select(l => l.Url));
         context.Response.Headers["Pagination-Offset"] = offset.ToString();
         context.Response.Headers["Pagination-Limit"] = limit.ToString();
         context.Response.Headers["Pagination-Total"] = total.ToString();
      }
   }
}
