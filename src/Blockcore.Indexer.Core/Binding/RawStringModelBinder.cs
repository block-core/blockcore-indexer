using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Blockcore.Indexer.Core.Binding
{
   public class RawStringModelBinder : IModelBinder
   {
      /// <inheritdoc />
      public Task BindModelAsync(ModelBindingContext bindingContext)
      {
         if (bindingContext == null)
         {
            throw new ArgumentNullException("bindingContext");
         }

         using (var memoryStream = new MemoryStream())
         {
            bindingContext.HttpContext.Request.Body.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using (var rdr = new StreamReader(memoryStream))
            {
               string resut = rdr.ReadToEnd();
               bindingContext.Result = ModelBindingResult.Success(resut);
               return Task.CompletedTask;
            }
         }
      }
   }
}
