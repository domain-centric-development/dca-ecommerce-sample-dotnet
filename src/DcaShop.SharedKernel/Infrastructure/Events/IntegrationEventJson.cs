using System.Text.Json;
using System.Text.Json.Serialization;

using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>The JSON form of an integration event snapshot: the outbox writes it and reads it back in it.</summary>
/// <remarks>
/// A <see cref="Currency"/> travels as its ISO 4217 code (<c>"EUR"</c>), the form the Java twin's payload has,
/// rather than as an object around the code.
/// </remarks>
public static class IntegrationEventJson
{
    /// <summary>The options every snapshot is written and read with.</summary>
    public static JsonSerializerOptions Options { get; } = Create(writeIndented: false);

    /// <summary>The same options, indented — for showing a payload to a person.</summary>
    public static JsonSerializerOptions Indented { get; } = Create(writeIndented: true);

    private static JsonSerializerOptions Create(bool writeIndented)
    {
        var options = new JsonSerializerOptions { WriteIndented = writeIndented };
        options.Converters.Add(new CurrencyCodeConverter());
        return options;
    }

    private sealed class CurrencyCodeConverter : JsonConverter<Currency>
    {
        public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Currency.Of(reader.GetString() ?? throw new JsonException("A currency must be an ISO 4217 code"));

        public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Code);
    }
}