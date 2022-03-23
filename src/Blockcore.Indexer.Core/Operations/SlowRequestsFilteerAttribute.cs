using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ConcurrentCollections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Blockcore.Indexer.Core.Operations;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SlowRequestsFilteerAttribute : ActionFilterAttribute
{
   readonly ConcurrentHashSet<string> cache;
   public SlowRequestsFilteerAttribute()
   {
      cache = new ConcurrentHashSet<string>();
   }

   public override void OnActionExecuting(ActionExecutingContext c)
   {
      string key = ParseKey(c.HttpContext.Request.Method, c.ActionArguments.Values);
      bool allowExecute = false;

      if (!cache.Contains(key))
      {
         cache.Add(key);
         allowExecute = true;

         c.HttpContext.Items.Add("CacheKey",key);
      }

      if (!allowExecute)
      {
         c.Result = new AcceptedResult();
         c.HttpContext.Response.StatusCode = (int)HttpStatusCode.Accepted;
      }
   }

   public override void OnActionExecuted(ActionExecutedContext c)
   {
      string key = c.HttpContext.Items["CacheKey"].ToString();
      if (!cache.TryRemove(key))
         cache.Clear();
   }

   static string ParseKey(string methodName, IEnumerable<object> parameters)
   {
      var key = new StringBuilder(methodName);
      foreach (string parameter in parameters)
      {
         key.Append(parameter);
      }

      return key.ToString();
   }
}
