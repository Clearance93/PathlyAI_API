using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pathly_Services
{
    /// <summary>
    /// Custom converter that handles conversion of string or array values to a List<string>.
    /// This prevents deserialization failures when the JSON contains a string instead of an array.
    /// </summary>
    public class StringToListConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var stringValue = reader.GetString();
                    return string.IsNullOrWhiteSpace(stringValue)
                        ? new List<string>()
                        : new List<string> { stringValue };

                case JsonTokenType.StartArray:
                    var list = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var value = reader.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                list.Add(value);
                            }
                        }
                    }
                    return list;

                case JsonTokenType.Null:
                    return new List<string>();

                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing List<string>.");
            }
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
