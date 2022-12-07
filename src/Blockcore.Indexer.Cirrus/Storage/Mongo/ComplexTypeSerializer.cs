using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo;

public class ComplexTypeSerializer : SerializerBase<object>
{
   public override object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
   {
      var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
      var document = serializer.Deserialize(context, args);

      var bsonDocument = document.ToBsonDocument();

      var result = BsonExtensionMethods.ToJson(bsonDocument,new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
      return JsonConvert.DeserializeObject<IDictionary<string, object>>(result);
   }

   public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
   {
      var jsonDocument = JsonConvert.SerializeObject(value);
      var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(jsonDocument);

      var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
      serializer.Serialize(context, bsonDocument.AsBsonValue);
   }
}
