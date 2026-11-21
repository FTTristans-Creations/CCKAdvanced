using System;
using System.Collections.Generic;
using CVR.Newtonsoft.Json;
using CVR.Newtonsoft.Json.Linq;

namespace CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems
{
    public class ProblemJsonConverter : JsonConverter<Problem>
    {
        private static readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>
        {
            { $"{UploadError.Category}.File.TooLarge", typeof(FileSizeProblem) },
            { $"{UploadError.Category}.File.InvalidType", typeof(InvalidFileTypeProblem) },
            { $"{UploadError.Category}.File.MissingEntry", typeof(MissingPayloadEntryProblem) },
            { $"{UploadError.Category}.File.UnsupportedVersion", typeof(UnsupportedVersionProblem) },
        };

        public override Problem ReadJson(JsonReader reader, Type objectType, Problem existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string type = jo["Type"]?.ToString();

            if (type != null && TypeMap.TryGetValue(type, out var targetType))
            {
                return (Problem)jo.ToObject(targetType, serializer);
            }

            // Create a serializer without this converter to avoid recursion
            var noConverterSerializer = new JsonSerializer();
            foreach (var conv in serializer.Converters)
            {
                if (conv is not ProblemJsonConverter)
                    noConverterSerializer.Converters.Add(conv);
            }

            return jo.ToObject<Problem>(noConverterSerializer);
        }


        public override void WriteJson(JsonWriter writer, Problem value, JsonSerializer serializer)
        {
            JObject jo = JObject.FromObject(value, serializer);
            jo.WriteTo(writer);
        }
    }
}