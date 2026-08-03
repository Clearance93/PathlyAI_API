using Pathly_DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pathly_Services
{
    public class CareerMatchListConverter : JsonConverter<List<CareerMatchDto>?>
    {
        public override List<CareerMatchDto>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                return null;

            var result = new List<CareerMatchDto>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    result.Add(new CareerMatchDto { Title = reader.GetString() });
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var career = JsonSerializer.Deserialize<CareerMatchDto>(ref reader, options);
                    if (career != null) result.Add(career);
                }
                else
                {
                    reader.Skip();
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<CareerMatchDto>? value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, options);
    }
}
