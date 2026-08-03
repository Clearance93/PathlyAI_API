using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pathly_Services
{
    /// <summary>
    /// Factory that converts JSON arrays to comma-separated strings for string? properties.
    /// Handles cases where the AI returns an array instead of a string.
    /// </summary>
    public class ArrayToStringConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(string);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => new StringArrayConverter();

        private class StringArrayConverter : JsonConverter<string?>
        {
            public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return reader.GetString();

                    case JsonTokenType.StartArray:
                        var items = new List<string>();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                var val = reader.GetString();
                                if (!string.IsNullOrWhiteSpace(val))
                                    items.Add(val);
                            }
                        }
                        return string.Join(", ", items);

                    case JsonTokenType.Null:
                        return null;

                    default:
                        reader.Skip();
                        return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
            {
                if (value is null) writer.WriteNullValue();
                else writer.WriteStringValue(value);
            }
        }
    }
}
