using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Blockcore.Indexer.Core.Binding
{
   public class RawStringModelBinder : IModelBinder
   {
      /// <inheritdoc />
      public async Task BindModelAsync(ModelBindingContext bindingContext)
      {
         if (bindingContext == null)
         {
            throw new ArgumentNullException("bindingContext");
         }

         await using (var memoryStream = new MemoryStream())
         {
            await bindingContext.HttpContext.Request.Body.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using (var rdr = new StreamReader(memoryStream))
            {
               string resut = await rdr.ReadToEndAsync();
               bindingContext.Result = ModelBindingResult.Success(resut);
            }
         }
      }
   }
}
