using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Blockcore.Indexer.Core.Paging
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
      //private readonly RequestContextDelegate requestQuery;
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
         PageLink link = new PageLink
         {
            Offset = offset,
            Limit = limit,
            Rel = rel
         };

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
         /*
         SPECIFICATION:

         2022-02-13: The initial implementation of paging was not using offset/limit,
         so the rule to return the last page when page 0 was provided, does not work
         correctly with offset/limit instead of pages. To return the latest set of data,
         the offset must bet set to null, not 0.

         On the last page of data, the "next" link should not be returned.
         On the first page of data, the "previous" link should not be returned.
         "first" and "last" are always returned no matter what.
         If the limit is higher than total available items, handle this accordingly.
         The array of links is always ordered: "first", "last", "previous" and "next".
         */

         List<PageLink> links = new List<PageLink>();

         links.Add(Create(0, limit, "first"));
         links.Add(Create((total - limit), limit, "last"));

         // If the total is less than limit, we won't be rendering next/previous links.
         if (limit > total)
         {
            return links;
         }

         // If offset queried is higher than 0, we'll always include the previous link.
         if (offset > 0)
         {
            long offsetPrevious = offset - limit;

            // If offset previous is lower than 0, make sure it's 0.
            if (offsetPrevious < 0)
            {
               offsetPrevious = 0;
            }

            links.Add(Create(offsetPrevious, limit, "previous"));
         }

         long offsetNext = offset + limit;

         if (offsetNext < total) // Due to 0 index we must +1.
         {
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
