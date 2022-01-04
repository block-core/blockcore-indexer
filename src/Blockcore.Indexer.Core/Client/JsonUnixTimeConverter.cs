namespace Blockcore.Indexer.Client
{
   using System;
   using Blockcore.Indexer.Extensions;
   using Newtonsoft.Json;


   /// <summary>
   /// json Unix time converter.
   /// </summary>
   public class JsonUnixTimeConverter : JsonConverter
   {
      public override bool CanConvert(Type objectType)
      {
         throw new NotImplementedException();
      }

      public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
      {
         if (long.TryParse(reader.Value.ToString(), out long ret))
         {
            return ret;
         }

         if (DateTime.TryParse(reader.Value.ToString(), out DateTime dt))
         {
            return UnixUtils.DateToUnixTimestamp(dt);
         }

         return ret;
      }

      public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
      {
         throw new NotImplementedException();
      }
   }
}
