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
                    if (TimeOnly.TryParseExact(timeString, "HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var withMs))
                        return withMs;

                    if (TimeOnly.TryParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var withoutMs))
                        return withoutMs;

                    if (TimeOnly.TryParseExact(timeString, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hoursOnly))
                        return hoursOnly;
                }
            }

            throw new JsonException("Expected a time string in HH:mm:ss or HH:mm:ss.fff format.");
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