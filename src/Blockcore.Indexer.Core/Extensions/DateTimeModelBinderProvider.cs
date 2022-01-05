using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Blockcore.Indexer.Core.Extensions
{
   /// <summary>
   /// Ensures that the date supplied to the REST API is parsed to become an Kind.Utc instance.
   /// </summary>
   public class DateTimeModelBinderProvider : IModelBinderProvider
   {
      public IModelBinder GetBinder(ModelBinderProviderContext context)
      {
         if (context == null)
         {
            throw new ArgumentNullException(nameof(context));
         }

         if (context.Metadata.ModelType == typeof(DateTime) ||
             context.Metadata.ModelType == typeof(DateTime?))
         {
            return new DateTimeModelBinder();
         }

         return null;
      }
   }
}
