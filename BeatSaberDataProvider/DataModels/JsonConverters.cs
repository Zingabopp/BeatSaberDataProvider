using BeatSaberDataProvider.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BeatSaberDataProvider.DataModels
{
    // From: https://stackoverflow.com/a/55768479
    public class IntegerWithCommasConverter : JsonConverter<int>
    {
        public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot unmarshal int");
            }
            if (reader.TokenType == JsonToken.Integer)
                return Convert.ToInt32(reader.Value);
            var value = (string) reader.Value;
            const NumberStyles style = NumberStyles.AllowThousands;
            var result = int.Parse(value, style, CultureInfo.InvariantCulture);
            return result;
        }

        public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }

    public class EmptyArrayOrDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<string, object>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Object)
            {
                return token.ToObject(objectType);
            }
            else if (token.Type == JTokenType.Array)
            {
                if (!token.HasValues)
                {
                    // create empty dictionary
                    return Activator.CreateInstance(objectType);
                }
                // Handles case where Beat Saver gives the slashstat in the form of an array.
                if (objectType == typeof(Dictionary<string, int>))
                {
                    var retDict = new Dictionary<string, int>();
                    for (int i = 0; i < token.Count(); i++)
                    {
                        retDict.Add(i.ToString(), (int) token.ElementAt(i));
                    }
                    return retDict;
                }
            }
            //throw new JsonSerializationException($"{objectType.ToString()} or empty array expected, received a {token.Type.ToString()}");
            Logger.Warning($"{objectType.ToString()} or empty array expected, received a {token.Type.ToString()}");
            return Activator.CreateInstance(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// Allows you to specify nested properties in the [JsonProperty()] attribute.
    /// From https://dotnetfiddle.net/F8C8U8
    /// </summary>
    public class JsonPathConverter : JsonConverter
    {
        private static readonly Regex PROPERTY_CHECK = new Regex(@"^[a-zA-Z0-9_.-]+$", RegexOptions.Compiled);
        /// <inheritdoc />
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            object targetObj = Activator.CreateInstance(objectType);

            foreach (PropertyInfo prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                JsonPropertyAttribute att = prop.GetCustomAttributes(true)
                                                .OfType<JsonPropertyAttribute>()
                                                .FirstOrDefault();

                string jsonPath = att != null ? att.PropertyName : prop.Name;

                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath);
                }

                if (!PROPERTY_CHECK.IsMatch(jsonPath))
                {
                    throw new InvalidOperationException("JProperties of JsonPathConverter can have only letters, numbers, underscores, hyphens and dots but name was '" + jsonPath + "'."); // Array operations not permitted
                }

                JToken token = jo.SelectToken(jsonPath);
                if (token != null && prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    if (token.HasValues)
                    {
                        var listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(prop.PropertyType.GenericTypeArguments);

                    }
                }
                else
                {
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        object value = token.ToObject(prop.PropertyType, serializer);
                        prop.SetValue(targetObj, value, null);
                    }
                }
            }

            return targetObj;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            // CanConvert is not called when [JsonConverter] attribute is used
            return objectType.GetCustomAttributes(true).OfType<JsonPathConverter>().Any();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var properties = value.GetType().GetRuntimeProperties().Where(p => p.CanRead && p.CanWrite);
            JObject main = new JObject();
            foreach (PropertyInfo prop in properties)
            {
                JsonPropertyAttribute att = prop.GetCustomAttributes(true)
                    .OfType<JsonPropertyAttribute>()
                    .FirstOrDefault();

                string jsonPath = att != null ? att.PropertyName : prop.Name;
                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath);
                }

                var nesting = jsonPath.Split('.');
                JObject lastLevel = main;

                for (int i = 0; i < nesting.Length; i++)
                {
                    if (i == nesting.Length - 1)
                    {
                        lastLevel[nesting[i]] = new JValue(prop.GetValue(value));
                    }
                    else
                    {
                        if (lastLevel[nesting[i]] == null)
                        {
                            lastLevel[nesting[i]] = new JObject();
                        }

                        lastLevel = (JObject)lastLevel[nesting[i]];
                    }
                }
            }

            serializer.Serialize(writer, main);
        }
    }
}
