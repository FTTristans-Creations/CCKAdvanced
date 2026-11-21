using System;
using System.Collections.Generic;
using CVR.Newtonsoft.Json;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form
{
    public class ContentTagsConverter : JsonConverter<ContentTags>
    {
        public override ContentTags ReadJson(JsonReader reader, Type objectType, ContentTags existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Expected start of array");
            var tags = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;
                if (reader.TokenType != JsonToken.String)
                    throw new JsonSerializationException("Expected string");
                var tag = (string?)reader.Value;
                if (!string.IsNullOrEmpty(tag))
                    tags.Add(tag);
            }
            return ContentTags.FromStringArray(tags);
        }
        public override void WriteJson(JsonWriter writer, ContentTags value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            var values = value.ToStringArray();
            foreach (var s in values)
            {
                writer.WriteValue(s);
            }
            writer.WriteEndArray();
        }
    }
}