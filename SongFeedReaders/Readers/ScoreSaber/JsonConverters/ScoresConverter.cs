using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.ScoreSaber.JsonConverters
{
    public class ScoresConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(string);

        public override object ReadJson(JsonReader reader, Type t, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return 0;
            string? value = serializer.Deserialize<string>(reader);
            if (value == null || value.Length == 0)
                return 0;
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            serializer.Serialize(writer, untypedValue);
        }

        public static readonly ScoresConverter Singleton = new ScoresConverter();
    }
}
