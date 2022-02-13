using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Indexer.Core.Paging;
using Xunit;

namespace Blockcore.Indexer.Tests
{
   public class PagingTests
   {
      [Fact]
      public void VerifyPaging()
      {
         static string RequestPath()
         {
            return "/api/query/richlist";
         }

         static string RequestQuery()
         {
            return "?offset=38&limit=10";
         }

         PagingLinkBuilder linkBuilder = new PagingLinkBuilder(RequestPath, RequestQuery);

         // 10 offset, 10 limit, 20 total should not render a next link.
         List<PageLink> links = linkBuilder.Links(10, 10, 20);
         Assert.Equal(3, links.Count);

         Assert.Contains("first", links[0].Rel);
         Assert.Equal(10, links[0].Limit);
         Assert.Equal(0, links[0].Offset);

         Assert.Contains("last", links[1].Rel);
         Assert.Equal(10, links[1].Limit);
         Assert.Equal(10, links[1].Offset);

         Assert.Contains("previous", links[2].Rel);
         Assert.Equal(10, links[2].Limit);
         Assert.Equal(0, links[2].Offset);

         // 0 offset, 10 limit, 100 total. Normal behavior.
         links = linkBuilder.Links(0, 10, 100);
         Assert.Equal(3, links.Count);

         Assert.Contains("first", links[0].Rel);
         Assert.Equal(10, links[0].Limit);
         Assert.Equal(0, links[0].Offset);

         Assert.Contains("last", links[1].Rel);
         Assert.Equal(10, links[1].Limit);
         Assert.Equal(90, links[1].Offset);

         Assert.Contains("next", links[2].Rel);
         Assert.Equal(10, links[2].Limit);
         Assert.Equal(10, links[2].Offset);

         // Take next page based on previous
         links = linkBuilder.Links(links[2].Offset, links[2].Limit, 100);
         Assert.Equal(4, links.Count); // Now we should have all 4 links.
         Assert.Equal(20, links[3].Offset);

         // Take next page based on previous
         links = linkBuilder.Links(links[3].Offset, links[3].Limit, 100);
         Assert.Equal(30, links[3].Offset);

         // Verify the second last page.
         links = linkBuilder.Links(81, links[3].Limit, 100);
         Assert.Equal(91, links[3].Offset);

         // Verify the last page is not returning a next link.
         links = linkBuilder.Links(91, links[3].Limit, 100);
         Assert.Equal(3, links.Count); // At the end we should only have 3 links, as in the beginning.
         Assert.Equal(81, links[2].Offset);
      }
   }
}
