using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Blockcore.Indexer.Core.Extensions
{
   /// <summary>
   /// Ensures that the date supplied to the REST API is parsed to become an Kind.Utc instance.
   /// </summary>
   public class DateTimeModelBinder : IModelBinder
   {
      public Task BindModelAsync(ModelBindingContext bindingContext)
      {
         if (bindingContext == null)
         {
            throw new ArgumentNullException(nameof(bindingContext));
         }

         // Try to fetch the value of the argument by name
         string modelName = bindingContext.ModelName;

         ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

         if (valueProviderResult == ValueProviderResult.None)
         {
            return Task.CompletedTask;
         }

         bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

         string dateStr = valueProviderResult.FirstValue;

         //DateTime.TryParse(dateStr, out DateTime date);
         bool parsed = DateTime.TryParse(dateStr, null, DateTimeStyles.AssumeUniversal, out DateTime date);

         if (parsed)
         {
            // Make sure we convert the date to UTC.
            date = date.ToUniversalTime();
            bindingContext.Result = ModelBindingResult.Success(date);
         }

         return Task.CompletedTask;
      }
   }
}
