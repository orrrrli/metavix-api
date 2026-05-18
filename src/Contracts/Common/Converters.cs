using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contracts.Common;

public class Converters
{
    public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? timeString = reader.GetString();

                if (timeString != null)
                {
                    return TimeOnly.ParseExact(timeString, "HH:mm:ss.fff", CultureInfo.InvariantCulture);
                }
            }

            throw new JsonException("Expected a string.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("HH:mm:ss.fff"));
        }
    }

    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            string? date = reader.GetString();
            return DateOnly.Parse(date ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("dd/MM/yyyy"));
        }
    }
}