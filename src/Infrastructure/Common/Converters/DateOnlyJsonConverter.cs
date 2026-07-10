using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Common.Converters;

public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "dd/MM/yyyy";

    // Formatos aceptados al leer del body de un request. El primero es
    // el formato que el backend usa al serializar (consistente consigo
    // mismo). El segundo es el formato estándar de los inputs HTML5
    // `type="date"` y el que el frontend declara en sus tipos
    // (ver metavix-app/src/types/daily-record.ts:31, insulin-dm1.ts:24).
    private static readonly string[] AcceptedReadFormats = { "dd/MM/yyyy", "yyyy-MM-dd" };

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (value is null) return default;

        if (DateOnly.TryParseExact(
                value,
                AcceptedReadFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            return result;
        }

        throw new JsonException(
            $"DateOnly inválido: '{value}'. "
            + $"Formatos aceptados: {string.Join(", ", AcceptedReadFormats)}.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
